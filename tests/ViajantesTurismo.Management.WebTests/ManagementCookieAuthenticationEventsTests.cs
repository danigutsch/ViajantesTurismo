using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>
/// Verifies sign-out removes protected user and exchanged-audience tokens for the current session.
/// </summary>
[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class ManagementCookieAuthenticationEventsTests
{
    [Fact]
    public async Task Signing_out_clears_default_and_parameterized_user_tokens()
    {
        // Arrange
        var tokenStoreContext = new ProtectedDistributedUserTokenStoreTestContext();
        var user = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-a");
        await tokenStoreContext.Store.StoreTokenAsync(
            user,
            ProtectedDistributedUserTokenStoreTestContext.CreateToken("source-access-token"),
            ct: Xunit.TestContext.Current.CancellationToken);
        await tokenStoreContext.Store.StoreTokenAsync(
            user,
            ProtectedDistributedUserTokenStoreTestContext.CreateToken("admin-access-token"),
            new UserTokenRequestParameters { Scope = Scope.Parse("admin-api") },
            Xunit.TestContext.Current.CancellationToken);
        var httpContext = new DefaultHttpContext { User = user };
        var signingOutContext = new CookieSigningOutContext(
            httpContext,
            new AuthenticationScheme(
                CookieAuthenticationDefaults.AuthenticationScheme,
                displayName: null,
                handlerType: typeof(CookieAuthenticationHandler)),
            new CookieAuthenticationOptions(),
            properties: null,
            new CookieOptions());
        var events = new ManagementCookieAuthenticationEvents(tokenStoreContext.Store, tokenStoreContext.AudienceTokenStore);

        // Act
        await events.SigningOut(signingOutContext);
        var cachedValue = await tokenStoreContext.Cache.GetAsync(
            ProtectedDistributedUserTokenStoreTestContext.GetCacheKey("session-a"),
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        cachedValue.ShouldBeNull();
    }

    [Fact]
    public async Task Signing_out_clears_exchanged_tokens_for_the_current_source_token()
    {
        // Arrange
        const string sourceAccessToken = "source-access-token";
        const string otherSourceAccessToken = "other-source-access-token";
        var tokenStoreContext = new ProtectedDistributedUserTokenStoreTestContext();
        var user = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-a");
        await tokenStoreContext.Store.StoreTokenAsync(
            user,
            ProtectedDistributedUserTokenStoreTestContext.CreateToken(sourceAccessToken),
            ct: Xunit.TestContext.Current.CancellationToken);
        var audiences = new[] { ApiAudienceNames.Admin, ApiAudienceNames.Catalog, ApiAudienceNames.Branding };
        foreach (var audience in audiences)
        {
            await tokenStoreContext.Cache.SetAsync(
                AudienceTokenExchangeTestHost.GetAudienceTokenCacheKey(audience, sourceAccessToken),
                [0x01],
                new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions(),
                Xunit.TestContext.Current.CancellationToken);
            await tokenStoreContext.Cache.SetAsync(
                AudienceTokenExchangeTestHost.GetAudienceTokenCacheKey(audience, otherSourceAccessToken),
                [0x02],
                new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions(),
                Xunit.TestContext.Current.CancellationToken);
        }

        var httpContext = new DefaultHttpContext { User = user };
        var signingOutContext = new CookieSigningOutContext(
            httpContext,
            new AuthenticationScheme(
                CookieAuthenticationDefaults.AuthenticationScheme,
                displayName: null,
                handlerType: typeof(CookieAuthenticationHandler)),
            new CookieAuthenticationOptions(),
            properties: null,
            new CookieOptions());
        var events = new ManagementCookieAuthenticationEvents(tokenStoreContext.Store, tokenStoreContext.AudienceTokenStore);

        // Act
        await events.SigningOut(signingOutContext);
        var removedAdminEntry = await tokenStoreContext.Cache.GetAsync(
            AudienceTokenExchangeTestHost.GetAudienceTokenCacheKey(ApiAudienceNames.Admin, sourceAccessToken),
            Xunit.TestContext.Current.CancellationToken);
        var removedCatalogEntry = await tokenStoreContext.Cache.GetAsync(
            AudienceTokenExchangeTestHost.GetAudienceTokenCacheKey(ApiAudienceNames.Catalog, sourceAccessToken),
            Xunit.TestContext.Current.CancellationToken);
        var removedBrandingEntry = await tokenStoreContext.Cache.GetAsync(
            AudienceTokenExchangeTestHost.GetAudienceTokenCacheKey(ApiAudienceNames.Branding, sourceAccessToken),
            Xunit.TestContext.Current.CancellationToken);
        var remainingAdminEntry = await tokenStoreContext.Cache.GetAsync(
            AudienceTokenExchangeTestHost.GetAudienceTokenCacheKey(ApiAudienceNames.Admin, otherSourceAccessToken),
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        removedAdminEntry.ShouldBeNull();
        removedCatalogEntry.ShouldBeNull();
        removedBrandingEntry.ShouldBeNull();
        remainingAdminEntry.ShouldNotBeNull();
    }

    [Fact]
    public async Task Signing_out_clears_user_tokens_when_exchanged_token_cleanup_fails()
    {
        // Arrange
        var innerCache = new Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(
                new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions()));
        var cache = new ThrowingRemoveDistributedCache(
            innerCache,
            shouldThrowOnRemove: static (key, _) => key.StartsWith("management-audience-token:", StringComparison.Ordinal));
        var tokenStoreContext = new ProtectedDistributedUserTokenStoreTestContext(cache);
        var user = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-a");
        await tokenStoreContext.Store.StoreTokenAsync(
            user,
            ProtectedDistributedUserTokenStoreTestContext.CreateToken("source-access-token"),
            ct: Xunit.TestContext.Current.CancellationToken);
        await tokenStoreContext.Cache.SetAsync(
            AudienceTokenExchangeTestHost.GetAudienceTokenCacheKey(ApiAudienceNames.Admin, "source-access-token"),
            [0x01],
            new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions(),
            Xunit.TestContext.Current.CancellationToken);
        var httpContext = new DefaultHttpContext { User = user };
        var signingOutContext = new CookieSigningOutContext(
            httpContext,
            new AuthenticationScheme(
                CookieAuthenticationDefaults.AuthenticationScheme,
                displayName: null,
                handlerType: typeof(CookieAuthenticationHandler)),
            new CookieAuthenticationOptions(),
            properties: null,
            new CookieOptions());
        var events = new ManagementCookieAuthenticationEvents(tokenStoreContext.Store, tokenStoreContext.AudienceTokenStore);

        // Act
        await events.SigningOut(signingOutContext);
        var userTokenEntry = await tokenStoreContext.Cache.GetAsync(
            ProtectedDistributedUserTokenStoreTestContext.GetCacheKey("session-a"),
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        userTokenEntry.ShouldBeNull();
    }

    [Fact]
    public async Task Signing_out_with_a_stale_authenticated_principal_still_completes()
    {
        // Arrange
        var tokenStoreContext = new ProtectedDistributedUserTokenStoreTestContext();
        var user = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity("test"));
        var httpContext = new DefaultHttpContext { User = user };
        var signingOutContext = new CookieSigningOutContext(
            httpContext,
            new AuthenticationScheme(
                CookieAuthenticationDefaults.AuthenticationScheme,
                displayName: null,
                handlerType: typeof(CookieAuthenticationHandler)),
            new CookieAuthenticationOptions(),
            properties: null,
            new CookieOptions());
        var events = new ManagementCookieAuthenticationEvents(tokenStoreContext.Store, tokenStoreContext.AudienceTokenStore);

        // Act
        await events.SigningOut(signingOutContext);

        // Assert
        httpContext.User.Identity?.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public async Task Signing_out_continues_when_protected_token_cleanup_fails()
    {
        // Arrange
        var innerCache = new Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(
                new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions()));
        var cache = new ThrowingRemoveDistributedCache(innerCache);
        var tokenStoreContext = new ProtectedDistributedUserTokenStoreTestContext(cache);
        var user = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-a");
        var httpContext = new DefaultHttpContext { User = user };
        var signingOutContext = new CookieSigningOutContext(
            httpContext,
            new AuthenticationScheme(
                CookieAuthenticationDefaults.AuthenticationScheme,
                displayName: null,
                handlerType: typeof(CookieAuthenticationHandler)),
            new CookieAuthenticationOptions(),
            properties: null,
            new CookieOptions());
        var events = new ManagementCookieAuthenticationEvents(tokenStoreContext.Store, tokenStoreContext.AudienceTokenStore);

        // Act
        await events.SigningOut(signingOutContext);

        // Assert
        httpContext.User.Identity?.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public async Task Signing_out_continues_when_protected_token_cleanup_throws_an_unexpected_cache_exception()
    {
        // Arrange
        var innerCache = new Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(
                new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions()));
        var cache = new ThrowingRemoveDistributedCache(innerCache, new IOException("The cache is unavailable."));
        var tokenStoreContext = new ProtectedDistributedUserTokenStoreTestContext(cache);
        var user = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-a");
        var httpContext = new DefaultHttpContext { User = user };
        var signingOutContext = new CookieSigningOutContext(
            httpContext,
            new AuthenticationScheme(
                CookieAuthenticationDefaults.AuthenticationScheme,
                displayName: null,
                handlerType: typeof(CookieAuthenticationHandler)),
            new CookieAuthenticationOptions(),
            properties: null,
            new CookieOptions());
        var events = new ManagementCookieAuthenticationEvents(tokenStoreContext.Store, tokenStoreContext.AudienceTokenStore);

        // Act
        await events.SigningOut(signingOutContext);

        // Assert
        httpContext.User.Identity?.IsAuthenticated.ShouldBeTrue();
    }
}
