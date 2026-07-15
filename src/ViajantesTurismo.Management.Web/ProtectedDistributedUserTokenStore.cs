using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using SharedKernel.AspNetCore;
using SharedKernel.BuildingBlocks;

namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Stores Management BFF user tokens in protected distributed storage for Blazor Server circuits.
/// </summary>
internal sealed class ProtectedDistributedUserTokenStore : IUserTokenStore
{
    private const int MaximumTokenEntries = 16;

    private readonly IDistributedCache _cache;
    private readonly KeyBoundDataProtector _protector;
    private readonly TimeProvider _timeProvider;

    public ProtectedDistributedUserTokenStore(
        IDistributedCache cache,
        IDataProtectionProvider dataProtectionProvider,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(dataProtectionProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);

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

        var entries = await ReadEntries(session, ct) ?? new Dictionary<string, UserToken>(StringComparer.Ordinal);
        var parameterKey = GetParameterKey(parameters);
        if (!entries.ContainsKey(parameterKey) && entries.Count >= MaximumTokenEntries)
        {
            throw new InvalidOperationException("The management token session has too many token entries.");
        }

        entries[parameterKey] = token;
        await WriteEntries(session, entries, ct);
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
            await TryRemoveStaleEntry(session.CacheKey, ct);
            return TokenResult.Failure("The management token session has expired.");
        }

        var entries = await ReadEntries(session, ct);
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
            await TryRemoveStaleEntry(session.CacheKey, ct);
            return;
        }

        var entries = await ReadEntries(session, ct);
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
    }

    internal Task ClearAll(ClaimsPrincipal user, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(user);

        return _cache.RemoveAsync(GetSession(user).CacheKey, ct);
    }

    internal async Task<string?> GetSourceAccessToken(ClaimsPrincipal user, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(user);

        var entries = await ReadEntries(GetSession(user), ct);
        if (entries is null || !entries.TryGetValue(string.Empty, out var token))
        {
            return null;
        }

        return token.AccessToken.ToString();
    }

    private async Task<Dictionary<string, UserToken>?> ReadEntries(TokenSession session, CancellationToken ct)
    {
        var protectedEntries = await _cache.GetAsync(session.CacheKey, ct);
        if (protectedEntries is null)
        {
            return null;
        }

        try
        {
            return DeserializeEntries(_protector.Unprotect(session.CacheKey, protectedEntries));
        }
        catch (Exception exception) when (exception is ArgumentException or CryptographicException or EndOfStreamException or FormatException or InvalidDataException or OverflowException)
        {
            await TryRemoveStaleEntry(session.CacheKey, ct);
            return null;
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

    private async Task<bool> TryRemoveStaleEntry(string cacheKey, CancellationToken ct)
    {
        try
        {
            await _cache.RemoveAsync(cacheKey, ct);
            return true;
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(ct))
        {
            return false;
        }
    }

    private sealed record TokenSession(string CacheKey, DateTimeOffset ExpiresAt);
}
