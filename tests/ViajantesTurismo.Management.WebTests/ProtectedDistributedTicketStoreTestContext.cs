using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

internal sealed class ProtectedDistributedTicketStoreTestContext
{
    public ProtectedDistributedTicketStoreTestContext(IDistributedCache? cache = null)
    {
        Cache = cache ?? new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        Store = new ProtectedDistributedTicketStore(Cache, new EphemeralDataProtectionProvider());
    }

    public IDistributedCache Cache { get; }

    public ProtectedDistributedTicketStore Store { get; }

    public static AuthenticationTicket CreateTicket(DateTimeOffset? expiresUtc = null)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "Test Administrator")], "test");
        var properties = new AuthenticationProperties { ExpiresUtc = expiresUtc ?? DateTimeOffset.UtcNow.AddHours(1) };
        return new AuthenticationTicket(new ClaimsPrincipal(identity), properties, "test");
    }
}
