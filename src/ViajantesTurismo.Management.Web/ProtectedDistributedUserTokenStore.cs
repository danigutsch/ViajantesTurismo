using System.Buffers.Binary;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Npgsql;
using SharedKernel.AspNetCore;
using SharedKernel.BuildingBlocks;
using SharedKernel.Npgsql;

namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Stores Management BFF user tokens in protected distributed storage for Blazor Server circuits.
/// </summary>
internal sealed class ProtectedDistributedUserTokenStore : IUserTokenStore
{
    private const int MaximumTokenEntries = 16;
    private const string SessionMutationLockPurpose = "ViajantesTurismo.Management.Web.ProtectedDistributedUserTokenStore";
    private const string SessionRevocationKeySuffix = ":revoked";

    private readonly NpgsqlDataSource? _advisoryLockDataSource;
    private readonly IDistributedCache _cache;
    private readonly KeyBoundDataProtector _protector;
    private readonly TimeProvider _timeProvider;

    public ProtectedDistributedUserTokenStore(
        IDistributedCache cache,
        IDataProtectionProvider dataProtectionProvider,
        TimeProvider timeProvider,
        NpgsqlDataSource advisoryLockDataSource)
        : this(cache, dataProtectionProvider, timeProvider, advisoryLockDataSource, bypassAdvisoryLock: false)
    {
    }

    internal static ProtectedDistributedUserTokenStore CreateForTesting(
        IDistributedCache cache,
        IDataProtectionProvider dataProtectionProvider,
        TimeProvider timeProvider)
    {
        return new ProtectedDistributedUserTokenStore(
            cache,
            dataProtectionProvider,
            timeProvider,
            advisoryLockDataSource: null,
            bypassAdvisoryLock: true);
    }

    private ProtectedDistributedUserTokenStore(
        IDistributedCache cache,
        IDataProtectionProvider dataProtectionProvider,
        TimeProvider timeProvider,
        NpgsqlDataSource? advisoryLockDataSource,
        bool bypassAdvisoryLock)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(dataProtectionProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (!bypassAdvisoryLock)
        {
            ArgumentNullException.ThrowIfNull(advisoryLockDataSource);
        }

        _advisoryLockDataSource = advisoryLockDataSource;
        _cache = cache;
        _protector = new KeyBoundDataProtector(
            dataProtectionProvider,
            ManagementAuthenticationDefaults.UserTokenStoreProtectorPurpose);
        _timeProvider = timeProvider;
    }

    public async Task StoreTokenAsync(
        ClaimsPrincipal user,
        UserToken token,
        UserTokenRequestParameters? parameters = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(token);

        var session = GetSession(user);
        EnsureActive(session);
        EnsureSupported(token);

        await ExecuteSessionMutation(session, async () =>
        {
            EnsureActive(session);
            if (await IsSessionRevoked(session, ct))
            {
                throw new InvalidOperationException("The management token session has been revoked.");
            }

            var read = await ReadEntries(session, ct);
            var entries = read.Entries ?? new Dictionary<string, UserToken>(StringComparer.Ordinal);
            var parameterKey = GetParameterKey(parameters);
            if (!entries.ContainsKey(parameterKey) && entries.Count >= MaximumTokenEntries)
            {
                throw new InvalidOperationException("The management token session has too many token entries.");
            }

            entries[parameterKey] = token;
            await WriteEntries(session, entries, ct);
        }, ct);
    }

    public async Task<TokenResult<TokenForParameters>> GetTokenAsync(
        ClaimsPrincipal user,
        UserTokenRequestParameters? parameters = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var session = GetSession(user);
        if (session.ExpiresAt <= _timeProvider.GetUtcNow())
        {
            await TryRemoveStaleEntry(session, expectedProtectedEntries: null, ct);
            return TokenResult.Failure("The management token session has expired.");
        }

        if (await IsSessionRevoked(session, ct))
        {
            return TokenResult.Failure("No access token or refresh token is available.");
        }

        var read = await ReadEntries(session, ct);
        if (read.CorruptProtectedEntries is not null)
        {
            await TryRemoveStaleEntry(session, read.CorruptProtectedEntries, ct);
            return TokenResult.Failure("No access token or refresh token is available.");
        }

        var entries = read.Entries;
        if (entries is null)
        {
            return TokenResult.Failure("No access token or refresh token is available.");
        }

        var parameterKey = GetParameterKey(parameters);
        if (entries.TryGetValue(parameterKey, out var token))
        {
            return new TokenForParameters(token, CreateRefreshToken(token));
        }

        if (parameterKey.Length != 0
            && entries.TryGetValue(string.Empty, out var sourceToken)
            && CreateRefreshToken(sourceToken) is { } refreshToken)
        {
            return new TokenForParameters(refreshToken);
        }

        return TokenResult.Failure("No access token or refresh token is available.");
    }

    public async Task ClearTokenAsync(
        ClaimsPrincipal user,
        UserTokenRequestParameters? parameters = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var session = GetSession(user);
        if (session.ExpiresAt <= _timeProvider.GetUtcNow())
        {
            await TryRemoveStaleEntry(session, expectedProtectedEntries: null, ct);
            return;
        }

        await ExecuteSessionMutation(session, async () =>
        {
            var read = await ReadEntries(session, ct);
            if (read.CorruptProtectedEntries is not null)
            {
                await TryRemoveStaleEntryWithinMutation(session, read.CorruptProtectedEntries, ct);
                return;
            }

            var entries = read.Entries;
            if (entries is null)
            {
                return;
            }

            entries.Remove(GetParameterKey(parameters));
            if (entries.Count == 0)
            {
                await _cache.RemoveAsync(session.CacheKey, ct);
                return;
            }

            await WriteEntries(session, entries, ct);
        }, ct);
    }

    internal async Task<bool> ClearAll(ClaimsPrincipal user, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(user);

        var session = GetSession(user);
        return await ExecuteSessionMutation(session, async () =>
        {
            if (session.ExpiresAt > _timeProvider.GetUtcNow())
            {
                await WriteSessionRevocation(session, ct);
            }

            return await TryRemoveTokenEntries(session, ct);
        }, ct);
    }

    internal async Task<T> ExecuteForActiveSession<T>(
        ClaimsPrincipal user,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(operation);

        var session = GetSession(user);
        return await ExecuteSessionMutation(session, async () =>
        {
            EnsureActive(session);
            if (await IsSessionRevoked(session, ct))
            {
                throw new InvalidOperationException("The management token session has been revoked.");
            }

            return await operation(ct);
        }, ct);
    }

    private async Task<(Dictionary<string, UserToken>? Entries, byte[]? CorruptProtectedEntries)> ReadEntries(
        TokenSession session,
        CancellationToken ct)
    {
        var protectedEntries = await _cache.GetAsync(session.CacheKey, ct);
        if (protectedEntries is null)
        {
            return (Entries: null, CorruptProtectedEntries: null);
        }

        try
        {
            return (Entries: DeserializeEntries(_protector.Unprotect(session.CacheKey, protectedEntries)), CorruptProtectedEntries: null);
        }
        catch (Exception exception) when (exception is ArgumentException or CryptographicException or EndOfStreamException or FormatException or InvalidDataException or OverflowException)
        {
            return (Entries: null, CorruptProtectedEntries: protectedEntries);
        }
    }

    private Task WriteEntries(TokenSession session, IReadOnlyDictionary<string, UserToken> entries, CancellationToken ct)
    {
        var protectedEntries = _protector.Protect(session.CacheKey, SerializeEntries(entries));
        return _cache.SetAsync(
            session.CacheKey,
            protectedEntries,
            new DistributedCacheEntryOptions { AbsoluteExpiration = session.ExpiresAt },
            ct);
    }

    private Task WriteSessionRevocation(TokenSession session, CancellationToken ct)
    {
        return _cache.SetAsync(
            GetSessionRevocationKey(session),
            [0x01],
            new DistributedCacheEntryOptions { AbsoluteExpiration = session.ExpiresAt },
            ct);
    }

    private static byte[] SerializeEntries(IReadOnlyDictionary<string, UserToken> entries)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        writer.Write(entries.Count);
        foreach (var entry in entries)
        {
            writer.Write(entry.Key);
            WriteToken(writer, entry.Value);
        }

        writer.Flush();
        return stream.ToArray();
    }

    private static Dictionary<string, UserToken> DeserializeEntries(byte[] serializedEntries)
    {
        using var stream = new MemoryStream(serializedEntries);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        var entryCount = reader.ReadInt32();
        if (entryCount is < 0 or > MaximumTokenEntries)
        {
            throw new InvalidDataException("The protected user-token store entry count is invalid.");
        }

        var entries = new Dictionary<string, UserToken>(entryCount, StringComparer.Ordinal);
        for (var index = 0; index < entryCount; index++)
        {
            entries.Add(reader.ReadString(), ReadToken(reader));
        }

        if (stream.Position != stream.Length)
        {
            throw new InvalidDataException("The protected user-token store has unexpected trailing data.");
        }

        return entries;
    }

    private static void WriteToken(BinaryWriter writer, UserToken token)
    {
        writer.Write(token.AccessToken.ToString());
        writer.Write(token.Expiration.ToUnixTimeMilliseconds());
        writer.Write(token.ClientId.ToString());
        WriteNullableString(writer, token.AccessTokenType?.ToString());
        WriteNullableString(writer, token.RefreshToken?.ToString());
        WriteNullableString(writer, token.IdentityToken?.ToString());
        WriteNullableString(writer, token.Scope?.ToString());
    }

    private static UserToken ReadToken(BinaryReader reader)
    {
        var accessToken = AccessToken.Parse(reader.ReadString());
        var expiration = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64());
        var clientId = ClientId.Parse(reader.ReadString());
        var accessTokenType = ReadNullableString(reader);
        var refreshToken = ReadNullableString(reader);
        var identityToken = ReadNullableString(reader);
        var scope = ReadNullableString(reader);

        return new UserToken
        {
            AccessToken = accessToken,
            AccessTokenType = accessTokenType is null ? null : AccessTokenType.Parse(accessTokenType),
            ClientId = clientId,
            Expiration = expiration,
            RefreshToken = refreshToken is null ? null : RefreshToken.Parse(refreshToken),
            IdentityToken = identityToken is null ? null : IdentityToken.Parse(identityToken),
            Scope = scope is null ? null : Scope.Parse(scope)
        };
    }

    private static void WriteNullableString(BinaryWriter writer, string? value)
    {
        writer.Write(value is not null);
        if (value is not null)
        {
            writer.Write(value);
        }
    }

    private static string? ReadNullableString(BinaryReader reader)
    {
        return reader.ReadBoolean() ? reader.ReadString() : null;
    }

    private static UserRefreshToken? CreateRefreshToken(UserToken token)
    {
        return token.RefreshToken is { } refreshToken
            ? new UserRefreshToken(refreshToken, token.DPoPJsonWebKey)
            : null;
    }

    private static string GetParameterKey(UserTokenRequestParameters? parameters)
    {
        if (parameters?.Scope is null && parameters?.Resource is null)
        {
            return string.Empty;
        }

        return string.Concat(parameters.Scope, '\u001f', parameters.Resource);
    }

    private static TokenSession GetSession(ClaimsPrincipal user)
    {
        var sessionId = user.FindFirst(ManagementAuthenticationDefaults.UserTokenStoreSessionIdClaimType)?.Value;
        var expiresAtValue = user.FindFirst(ManagementAuthenticationDefaults.UserTokenStoreSessionExpiresAtClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(sessionId)
            || !DateTimeOffset.TryParse(expiresAtValue, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var expiresAt))
        {
            throw new InvalidOperationException("The management token session is unavailable.");
        }

        return new TokenSession(
            string.Concat(ManagementAuthenticationDefaults.UserTokenStoreKeyPrefix, sessionId),
            expiresAt);
    }

    private void EnsureActive(TokenSession session)
    {
        if (session.ExpiresAt <= _timeProvider.GetUtcNow())
        {
            throw new InvalidOperationException("The management token session has expired.");
        }
    }

    private static void EnsureSupported(UserToken token)
    {
        if (token.DPoPJsonWebKey is not null)
        {
            throw new InvalidOperationException("DPoP user-token storage is not configured.");
        }
    }

    private async Task ExecuteSessionMutation(TokenSession session, Func<Task> mutation, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        if (_advisoryLockDataSource is null)
        {
            await mutation();
            return;
        }

        await using var connection = await _advisoryLockDataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await PostgreSqlTransactionAdvisoryLock.Acquire(connection, transaction, GetSessionMutationLockKey(session), ct);
        await mutation();
        await transaction.CommitAsync(ct);
    }

    private async Task<T> ExecuteSessionMutation<T>(TokenSession session, Func<Task<T>> mutation, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        if (_advisoryLockDataSource is null)
        {
            return await mutation();
        }

        await using var connection = await _advisoryLockDataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await PostgreSqlTransactionAdvisoryLock.Acquire(connection, transaction, GetSessionMutationLockKey(session), ct);
        var result = await mutation();
        await transaction.CommitAsync(ct);
        return result;
    }

    private async Task<bool> TryRemoveStaleEntry(
        TokenSession session,
        byte[]? expectedProtectedEntries,
        CancellationToken ct)
    {
        try
        {
            await ExecuteSessionMutation(
                session,
                () => RemoveStaleEntry(session, expectedProtectedEntries, ct),
                ct);
            return true;
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(ct))
        {
            return false;
        }
    }

    private async Task<bool> TryRemoveStaleEntryWithinMutation(
        TokenSession session,
        byte[]? expectedProtectedEntries,
        CancellationToken ct)
    {
        try
        {
            await RemoveStaleEntry(session, expectedProtectedEntries, ct);
            return true;
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(ct))
        {
            return false;
        }
    }

    private async Task<bool> TryRemoveTokenEntries(TokenSession session, CancellationToken ct)
    {
        try
        {
            await _cache.RemoveAsync(session.CacheKey, ct);
            return true;
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(ct))
        {
            return false;
        }
    }

    private async Task RemoveStaleEntry(TokenSession session, byte[]? expectedProtectedEntries, CancellationToken ct)
    {
        if (expectedProtectedEntries is null)
        {
            await _cache.RemoveAsync(session.CacheKey, ct);
            return;
        }

        var currentProtectedEntries = await _cache.GetAsync(session.CacheKey, ct);
        if (currentProtectedEntries is not null && currentProtectedEntries.AsSpan().SequenceEqual(expectedProtectedEntries))
        {
            await _cache.RemoveAsync(session.CacheKey, ct);
        }
    }

    private async Task<bool> IsSessionRevoked(TokenSession session, CancellationToken ct)
    {
        return await _cache.GetAsync(GetSessionRevocationKey(session), ct) is not null;
    }

    private static long GetSessionMutationLockKey(TokenSession session)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{SessionMutationLockPurpose}:{session.CacheKey}"));
        return BinaryPrimitives.ReadInt64LittleEndian(hash);
    }

    private static string GetSessionRevocationKey(TokenSession session)
    {
        return string.Concat(session.CacheKey, SessionRevocationKeySuffix);
    }

    private sealed record TokenSession(string CacheKey, DateTimeOffset ExpiresAt);
}
