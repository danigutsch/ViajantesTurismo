using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Test host wiring the protected ticket store into cookie authentication with failing caches.</summary>
internal sealed class ProtectedDistributedTicketStoreCookieHandlerTestHost : IDisposable
{
    /// <summary>Name of the test authentication cookie.</summary>
    internal const string CookieName = "management-test";
    private readonly ThrowingRemoveDistributedCache? _ticketRemovalCache;

    private ProtectedDistributedTicketStoreCookieHandlerTestHost(
        IHost host,
        ThrowingRemoveDistributedCache? ticketRemovalCache)
    {
        Host = host;
        _ticketRemovalCache = ticketRemovalCache;
    }

    /// <summary>Cache that throws on ticket removal; throws when the host was not started with it.</summary>
    public ThrowingRemoveDistributedCache Cache => _ticketRemovalCache
        ?? throw new InvalidOperationException("The test host was not configured with a failing ticket cache.");

    /// <summary>The running test host.</summary>
    public IHost Host
    {
        get;
    }

    /// <summary>Starts a host whose cache throws when a ticket is removed.</summary>
    public static async Task<ProtectedDistributedTicketStoreCookieHandlerTestHost> StartWithFailingTicketRemoval(CancellationToken ct)
    {
        var innerCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var cache = new ThrowingRemoveDistributedCache(innerCache);

        return await Start(cache, cache, ct);
    }

    /// <summary>Starts a host whose cache throws when a session revocation marker is written.</summary>
    public static async Task<ProtectedDistributedTicketStoreCookieHandlerTestHost> StartWithFailingSessionRevocation(CancellationToken ct)
    {
        var innerCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var cache = new ThrowingSetDistributedCache(
            innerCache,
            static (key, _) => key.EndsWith(":revoked", StringComparison.Ordinal));

        return await Start(cache, ticketRemovalCache: null, ct: ct);
    }

    private static async Task<ProtectedDistributedTicketStoreCookieHandlerTestHost> Start(
        IDistributedCache cache,
        ThrowingRemoveDistributedCache? ticketRemovalCache,
        CancellationToken ct)
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(static services => services.AddTestRequestBindingDefaults())
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddDataProtection();
                    services.AddLogging();
                    services.AddSingleton(cache);
                    services.AddSingleton(TimeProvider.System);
                    services.AddSingleton<ITicketStore, ProtectedDistributedTicketStore>();
                    services.AddScoped<ProtectedDistributedAudienceTokenStore>();
                    services.AddScoped(serviceProvider =>
                        ProtectedDistributedUserTokenStore.CreateForTesting(
                            serviceProvider.GetRequiredService<IDistributedCache>(),
                            serviceProvider.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>(),
                            serviceProvider.GetRequiredService<TimeProvider>()));
                    services.AddScoped<ManagementCookieAuthenticationEvents>();
                    services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                        .AddCookie(options =>
                        {
                            options.Cookie.Name = CookieName;
                            options.EventsType = typeof(ManagementCookieAuthenticationEvents);
                        });
                    services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
                        .Configure<ITicketStore>((options, ticketStore) => options.SessionStore = ticketStore);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/sign-in", static async context =>
                        {
                            var user = new ClaimsPrincipal(new ClaimsIdentity(
                                [
                                    new Claim(ClaimTypes.Name, "Test Administrator"),
                                    new Claim(ManagementAuthenticationDefaults.UserTokenStoreSessionIdClaimType, "test-session"),
                                    new Claim(
                                        ManagementAuthenticationDefaults.UserTokenStoreSessionExpiresAtClaimType,
                                        TimeProvider.System.GetUtcNow().AddHours(1).ToString("O"))
                                ],
                                CookieAuthenticationDefaults.AuthenticationScheme));
                            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, user);
                        });
                        endpoints.MapPost("/sign-out", static context =>
                            context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme));
                    });
                }))
            .StartAsync(ct);

        return new ProtectedDistributedTicketStoreCookieHandlerTestHost(host, ticketRemovalCache);
    }

    /// <summary>Disposes the test host.</summary>
    public void Dispose()
    {
        Host.Dispose();
    }
}
