using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using SharedKernel.AspNetCore;
using SharedKernel.BuildingBlocks;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Stores exchanged backend access tokens outside the browser and removes them with their source token.
/// </summary>
internal sealed class ProtectedDistributedAudienceTokenStore
{
    private const string CacheKeyPrefix = "management-audience-token:";
    private const string ProtectorPurpose = "ViajantesTurismo.Management.Web.AudienceTokenStore.v2";
    private const int SourceAccessTokenFingerprintLength = 32;
    private static readonly TimeSpan RefreshBeforeExpiration = TimeSpan.FromMinutes(1);
    private static readonly string[] Audiences =
    [
        ApiAudienceNames.Admin,
        ApiAudienceNames.Catalog,
        ApiAudienceNames.Branding
    ];

    private readonly IDistributedCache _cache;
    private readonly KeyBoundDataProtector _protector;
    private readonly TimeProvider _timeProvider;

    public ProtectedDistributedAudienceTokenStore(
        IDistributedCache cache,
        IDataProtectionProvider dataProtectionProvider,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(dataProtectionProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _cache = cache;
        _protector = new KeyBoundDataProtector(dataProtectionProvider, ProtectorPurpose);
        _timeProvider = timeProvider;
    }

    internal async Task<string?> Get(
        string audience,
        ManagementTokenSession session,
        string sourceAccessToken,
        CancellationToken cancellationToken)
    {
        session.EnsureActive(_timeProvider);
        var cacheKey = GetCacheKey(audience, session);
        var sourceAccessTokenFingerprint = CreateSourceAccessTokenFingerprint(sourceAccessToken);
        var protectedEntry = await _cache.GetAsync(cacheKey, cancellationToken);
        if (protectedEntry is null)
        {
            return null;
        }

        try
        {
            using var stream = new MemoryStream(_protector.Unprotect(cacheKey, protectedEntry));
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64());
            var storedSourceAccessTokenFingerprint = reader.ReadBytes(SourceAccessTokenFingerprintLength);
            var accessToken = reader.ReadString();
            if (stream.Position != stream.Length
                || storedSourceAccessTokenFingerprint.Length != SourceAccessTokenFingerprintLength
                || !CryptographicOperations.FixedTimeEquals(storedSourceAccessTokenFingerprint, sourceAccessTokenFingerprint)
                || string.IsNullOrWhiteSpace(accessToken)
                || string.Equals(accessToken, sourceAccessToken, StringComparison.Ordinal)
                || expiresAt <= _timeProvider.GetUtcNow().Add(RefreshBeforeExpiration))
            {
                await TryRemoveStaleEntry(cacheKey, cancellationToken);
                return null;
            }

            return accessToken;
        }
        catch (Exception exception) when (exception is ArgumentException or CryptographicException or EndOfStreamException or FormatException or InvalidDataException or OverflowException)
        {
            await TryRemoveStaleEntry(cacheKey, cancellationToken);
            return null;
        }
    }

    internal Task Store(
        string audience,
        ManagementTokenSession session,
        string sourceAccessToken,
        string accessToken,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceAccessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        session.EnsureActive(_timeProvider);
        var cacheKey = GetCacheKey(audience, session);
        var cacheExpiresAt = expiresAt <= session.ExpiresAt ? expiresAt : session.ExpiresAt;
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(expiresAt.ToUnixTimeMilliseconds());
            writer.Write(CreateSourceAccessTokenFingerprint(sourceAccessToken));
            writer.Write(accessToken);
            writer.Flush();
        }

        return _cache.SetAsync(
            cacheKey,
            _protector.Protect(cacheKey, stream.ToArray()),
            new DistributedCacheEntryOptions { AbsoluteExpiration = cacheExpiresAt },
            cancellationToken);
    }

    internal Task ClearAll(ManagementTokenSession session, CancellationToken cancellationToken)
    {
        return Task.WhenAll(Audiences.Select(audience => _cache.RemoveAsync(GetCacheKey(audience, session), cancellationToken)));
    }

    internal static string GetCacheKey(string audience, ManagementTokenSession session)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);
        ArgumentException.ThrowIfNullOrWhiteSpace(session.Id);
        if (!Audiences.Contains(audience, StringComparer.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(audience), "The backend audience is not configured for token exchange.");
        }

        return string.Concat(CacheKeyPrefix, audience, ':', session.Id);
    }

    private static byte[] CreateSourceAccessTokenFingerprint(string sourceAccessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceAccessToken);

        return SHA256.HashData(Encoding.UTF8.GetBytes(sourceAccessToken));
    }

    private async Task<bool> TryRemoveStaleEntry(string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            await _cache.RemoveAsync(cacheKey, cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(cancellationToken))
        {
            return false;
        }
    }
}
