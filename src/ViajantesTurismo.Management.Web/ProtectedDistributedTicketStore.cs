using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using SharedKernel.BuildingBlocks;

namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Stores encrypted cookie authentication tickets outside the browser.
/// </summary>
internal sealed partial class ProtectedDistributedTicketStore : ITicketStore
{
    private const int CacheRemovalAttempts = 2;

    private readonly IDistributedCache cache;
    private readonly ILogger<ProtectedDistributedTicketStore> logger;
    private readonly IDataProtector protector;

    public ProtectedDistributedTicketStore(
        IDistributedCache cache,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<ProtectedDistributedTicketStore> logger)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(dataProtectionProvider);
        ArgumentNullException.ThrowIfNull(logger);

        this.cache = cache;
        this.logger = logger;
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

        var protectedTicket = GetProtector(key).Protect(TicketSerializer.Default.Serialize(ticket));
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
            var ticket = TicketSerializer.Default.Deserialize(GetProtector(key).Unprotect(protectedTicket));
            if (ticket is null || HasExpired(ticket))
            {
                await RemoveAsync(key).ConfigureAwait(false);
                return null;
            }

            return ticket;
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException or InvalidDataException)
        {
            await RemoveAsync(key).ConfigureAwait(false);
            return null;
        }
    }

    public Task RemoveAsync(string key)
    {
        return RemoveAsync(key, CancellationToken.None);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var firstFailure = await TryRemoveFromCache(key, cancellationToken).ConfigureAwait(false);
        if (firstFailure is null)
        {
            return;
        }

        var terminalFailure = await TryRemoveFromCache(key, cancellationToken).ConfigureAwait(false);
        if (terminalFailure is not null)
        {
            LogTicketCacheRemovalFailed(logger, CacheRemovalAttempts, terminalFailure.GetType().Name);
        }
    }

    [LoggerMessage(LogLevel.Error, "Management ticket cache removal failed after {AttemptCount} attempts. Failure type: {FailureType}.")]
    private static partial void LogTicketCacheRemovalFailed(ILogger logger, int attemptCount, string failureType);

    private async Task<Exception?> TryRemoveFromCache(string key, CancellationToken cancellationToken)
    {
        try
        {
            await cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            return null;
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(cancellationToken))
        {
            return exception;
        }
    }

    private static string CreateKey()
    {
        return string.Concat(
            ManagementAuthenticationDefaults.TicketStoreKeyPrefix,
            WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)));
    }

    private IDataProtector GetProtector(string key)
    {
        return protector.CreateProtector(key);
    }

    private static bool HasExpired(AuthenticationTicket ticket)
    {
        return ticket.Properties.ExpiresUtc is { } expiresUtc && expiresUtc <= DateTimeOffset.UtcNow;
    }
}
