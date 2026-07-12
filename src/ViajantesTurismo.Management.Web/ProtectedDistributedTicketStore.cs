using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;

namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Stores encrypted cookie authentication tickets outside the browser.
/// </summary>
internal sealed class ProtectedDistributedTicketStore : ITicketStore
{
    private readonly IDistributedCache cache;
    private readonly IDataProtector protector;

    public ProtectedDistributedTicketStore(IDistributedCache cache, IDataProtectionProvider dataProtectionProvider)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(dataProtectionProvider);

        this.cache = cache;
        protector = dataProtectionProvider.CreateProtector(ManagementAuthenticationDefaults.TicketStoreProtectorPurpose);
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        var key = CreateKey();
        await RenewAsync(key, ticket).ConfigureAwait(false);
        return key;
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(ticket);

        var protectedTicket = protector.Protect(TicketSerializer.Default.Serialize(ticket));
        return cache.SetAsync(
            key,
            protectedTicket,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = ticket.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.Add(ManagementAuthenticationDefaults.SessionLifetime)
            });
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var protectedTicket = await cache.GetAsync(key).ConfigureAwait(false);
        if (protectedTicket is null)
        {
            return null;
        }

        try
        {
            return TicketSerializer.Default.Deserialize(protector.Unprotect(protectedTicket));
        }
        catch (CryptographicException)
        {
            await cache.RemoveAsync(key).ConfigureAwait(false);
            return null;
        }
    }

    public Task RemoveAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return cache.RemoveAsync(key);
    }

    private static string CreateKey()
    {
        return string.Concat(
            ManagementAuthenticationDefaults.TicketStoreKeyPrefix,
            WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)));
    }
}
