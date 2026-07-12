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

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(ticket);

        if (HasExpired(ticket))
        {
            await cache.RemoveAsync(key).ConfigureAwait(false);
            return;
        }

        var protectedTicket = protector.Protect(TicketSerializer.Default.Serialize(ticket));
        await cache.SetAsync(
            key,
            protectedTicket,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = ticket.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.Add(ManagementAuthenticationDefaults.SessionLifetime)
            }).ConfigureAwait(false);
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
            var ticket = TicketSerializer.Default.Deserialize(protector.Unprotect(protectedTicket));
            if (ticket is null || HasExpired(ticket))
            {
                await cache.RemoveAsync(key).ConfigureAwait(false);
                return null;
            }

            return ticket;
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

    private static bool HasExpired(AuthenticationTicket ticket)
    {
        return ticket.Properties.ExpiresUtc is { } expiresUtc && expiresUtc <= DateTimeOffset.UtcNow;
    }
}
