using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using SharedKernel.AspNetCore;
using SharedKernel.BuildingBlocks;

namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Stores encrypted cookie authentication tickets outside the browser.
/// </summary>
internal sealed partial class ProtectedDistributedTicketStore : ITicketStore
{
    private const int CacheRemovalAttempts = 2;
    private const string ActiveRemovalCanceledMessage = "The management ticket removal was canceled.";
    private const string StaleCleanupCanceledMessage = "The management stale ticket cleanup was canceled.";

    private readonly IDistributedCache cache;
    private readonly ILogger<ProtectedDistributedTicketStore> logger;
    private readonly KeyBoundDataProtector protector;

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
        protector = new KeyBoundDataProtector(
            dataProtectionProvider,
            ManagementAuthenticationDefaults.TicketStoreProtectorPurpose);
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
            await TryRemoveStaleTicket(key, CancellationToken.None).ConfigureAwait(false);
            return;
        }

        var protectedTicket = protector.Protect(key, TicketSerializer.Default.Serialize(ticket));
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
            var ticket = TicketSerializer.Default.Deserialize(protector.Unprotect(key, protectedTicket));
            if (ticket is null || HasExpired(ticket))
            {
                await TryRemoveStaleTicket(key, CancellationToken.None).ConfigureAwait(false);
                return null;
            }

            return ticket;
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException or InvalidDataException)
        {
            await TryRemoveStaleTicket(key, CancellationToken.None).ConfigureAwait(false);
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

        try
        {
            var firstFailure = await TryRemoveFromCache(key, cancellationToken).ConfigureAwait(false);
            if (firstFailure is null)
            {
                return;
            }

            var terminalFailure = await TryRemoveFromCache(key, cancellationToken).ConfigureAwait(false);
            if (terminalFailure is not null)
            {
                LogTicketCacheRemovalFailed(logger, CacheRemovalAttempts, terminalFailure.GetType().Name);
                throw new InvalidOperationException("The management ticket could not be removed.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(ActiveRemovalCanceledMessage, cancellationToken);
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

    private async Task<bool> TryRemoveStaleTicket(string key, CancellationToken cancellationToken)
    {
        try
        {
            await cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(StaleCleanupCanceledMessage, cancellationToken);
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(cancellationToken))
        {
            return false;
        }
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
