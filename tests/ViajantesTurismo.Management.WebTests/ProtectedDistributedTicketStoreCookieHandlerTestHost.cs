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

internal sealed class ProtectedDistributedTicketStoreCookieHandlerTestHost : IDisposable
{
    internal const string CookieName = "management-test";

    private ProtectedDistributedTicketStoreCookieHandlerTestHost(IHost host, ThrowingRemoveDistributedCache cache)
    {
        Host = host;
        Cache = cache;
    }

    public ThrowingRemoveDistributedCache Cache { get; }

    public IHost Host { get; }

    public static async Task<ProtectedDistributedTicketStoreCookieHandlerTestHost> StartWithFailingTicketRemoval(CancellationToken ct)
    {
        var innerCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var cache = new ThrowingRemoveDistributedCache(innerCache);

        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddDataProtection();
                    services.AddLogging();
                    services.AddSingleton<IDistributedCache>(cache);
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
                                        DateTimeOffset.UtcNow.AddHours(1).ToString("O"))
                                ],
                                CookieAuthenticationDefaults.AuthenticationScheme));
                            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, user);
                        });
                        endpoints.MapPost("/sign-out", static context =>
                            context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme));
                    });
                }))
            .StartAsync(ct);

        return new ProtectedDistributedTicketStoreCookieHandlerTestHost(host, cache);
    }

    public void Dispose()
    {
        Host.Dispose();
    }
}
