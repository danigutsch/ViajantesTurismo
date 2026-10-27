using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Wires a protected distributed ticket store over an in-memory cache for tests.</summary>
internal sealed class ProtectedDistributedTicketStoreTestContext
{
    /// <summary>Creates the context, defaulting to an in-memory cache, null logger and system time.</summary>
    public ProtectedDistributedTicketStoreTestContext(
        IDistributedCache? cache = null,
        ILogger<ProtectedDistributedTicketStore>? logger = null,
        TimeProvider? timeProvider = null)
    {
        Cache = cache ?? new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        Store = new ProtectedDistributedTicketStore(
            Cache,
            new EphemeralDataProtectionProvider(),
            logger ?? NullLogger<ProtectedDistributedTicketStore>.Instance, timeProvider ?? TimeProvider.System);
    }

    /// <summary>Cache backing the store.</summary>
    public IDistributedCache Cache
    {
        get;
    }

    /// <summary>Ticket store under test.</summary>
    public ProtectedDistributedTicketStore Store
    {
        get;
    }

    /// <summary>Creates an authentication ticket expiring in one hour unless an expiry is given.</summary>
    public static AuthenticationTicket CreateTicket(DateTimeOffset? expiresUtc = null)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "Test Administrator")], "test");
        var properties = new AuthenticationProperties { ExpiresUtc = expiresUtc ?? TimeProvider.System.GetUtcNow().AddHours(1) };
        return new AuthenticationTicket(new ClaimsPrincipal(identity), properties, "test");
    }
}
