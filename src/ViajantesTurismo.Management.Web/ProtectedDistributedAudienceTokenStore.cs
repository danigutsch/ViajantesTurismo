using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
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
    private const string ProtectorPurpose = "ViajantesTurismo.Management.Web.AudienceTokenStore.v1";
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

    internal async Task<string?> Get(string audience, string sourceAccessToken, CancellationToken cancellationToken)
    {
        var cacheKey = GetCacheKey(audience, sourceAccessToken);
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
            var accessToken = reader.ReadString();
            if (stream.Position != stream.Length
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
        string sourceAccessToken,
        string accessToken,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var cacheKey = GetCacheKey(audience, sourceAccessToken);
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(expiresAt.ToUnixTimeMilliseconds());
            writer.Write(accessToken);
            writer.Flush();
        }

        return _cache.SetAsync(
            cacheKey,
            _protector.Protect(cacheKey, stream.ToArray()),
            new DistributedCacheEntryOptions { AbsoluteExpiration = expiresAt },
            cancellationToken);
    }

    internal Task ClearAll(string sourceAccessToken, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceAccessToken);

        return Task.WhenAll(Audiences.Select(audience => _cache.RemoveAsync(GetCacheKey(audience, sourceAccessToken), cancellationToken)));
    }

    internal static string GetCacheKey(string audience, string sourceAccessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceAccessToken);

        var sourceTokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(sourceAccessToken));
        return string.Concat(CacheKeyPrefix, audience, ':', WebEncoders.Base64UrlEncode(sourceTokenHash));
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
