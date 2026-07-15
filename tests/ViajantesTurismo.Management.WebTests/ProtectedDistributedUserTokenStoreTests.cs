using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>
/// Verifies protected server-side user-token storage for Blazor Server circuits.
/// </summary>
[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class ProtectedDistributedUserTokenStoreTests
{
    [Fact]
    public async Task Stores_tokens_protected_by_an_opaque_session_identifier()
    {
        // Arrange
        var context = new ProtectedDistributedUserTokenStoreTestContext();
        var user = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-a");
        var token = ProtectedDistributedUserTokenStoreTestContext.CreateToken("source-access-token");

        // Act
        await context.Store.StoreTokenAsync(user, token, ct: Xunit.TestContext.Current.CancellationToken);
        var storedValue = await context.Cache.GetAsync(
            ProtectedDistributedUserTokenStoreTestContext.GetCacheKey("session-a"),
            Xunit.TestContext.Current.CancellationToken);
        var result = await context.Store.GetTokenAsync(user, ct: Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Succeeded.ShouldBeTrue();
        var tokens = result.Token ?? throw new InvalidOperationException("The user token was not retrieved.");
        var storedToken = tokens.TokenForSpecifiedParameters ?? throw new InvalidOperationException("The access token was not retrieved.");
        storedToken.AccessToken.ToString().ShouldBe("source-access-token");
        var protectedValue = storedValue ?? throw new InvalidOperationException("The user token was not stored.");
        System.Text.Encoding.UTF8.GetString(protectedValue).ShouldNotContain("source-access-token", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Isolates_tokens_between_sessions_for_the_same_user()
    {
        // Arrange
        var context = new ProtectedDistributedUserTokenStoreTestContext();
        var firstUser = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-a");
        var secondUser = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-b");

        // Act
        await context.Store.StoreTokenAsync(
            firstUser,
            ProtectedDistributedUserTokenStoreTestContext.CreateToken("first-access-token"),
            ct: Xunit.TestContext.Current.CancellationToken);
        await context.Store.StoreTokenAsync(
            secondUser,
            ProtectedDistributedUserTokenStoreTestContext.CreateToken("second-access-token"),
            ct: Xunit.TestContext.Current.CancellationToken);
        var firstResult = await context.Store.GetTokenAsync(firstUser, ct: Xunit.TestContext.Current.CancellationToken);
        var secondResult = await context.Store.GetTokenAsync(secondUser, ct: Xunit.TestContext.Current.CancellationToken);

        // Assert
        var firstTokens = firstResult.Token ?? throw new InvalidOperationException("The first user token was not retrieved.");
        var secondTokens = secondResult.Token ?? throw new InvalidOperationException("The second user token was not retrieved.");
        var firstToken = firstTokens.TokenForSpecifiedParameters ?? throw new InvalidOperationException("The first access token was not retrieved.");
        var secondToken = secondTokens.TokenForSpecifiedParameters ?? throw new InvalidOperationException("The second access token was not retrieved.");
        firstToken.AccessToken.ToString().ShouldBe("first-access-token");
        secondToken.AccessToken.ToString().ShouldBe("second-access-token");
    }

    [Fact]
    public async Task Rejects_a_protected_token_entry_transplanted_from_another_session()
    {
        // Arrange
        var context = new ProtectedDistributedUserTokenStoreTestContext();
        var firstUser = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-a");
        var secondUser = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-b");
        await context.Store.StoreTokenAsync(
            firstUser,
            ProtectedDistributedUserTokenStoreTestContext.CreateToken("first-access-token"),
            ct: Xunit.TestContext.Current.CancellationToken);
        var transplantedValue = await context.Cache.GetAsync(
            ProtectedDistributedUserTokenStoreTestContext.GetCacheKey("session-a"),
            Xunit.TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("The source user-token entry was not stored.");
        await context.Cache.SetAsync(
            ProtectedDistributedUserTokenStoreTestContext.GetCacheKey("session-b"),
            transplantedValue,
            new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions(),
            Xunit.TestContext.Current.CancellationToken);

        // Act
        var result = await context.Store.GetTokenAsync(secondUser, ct: Xunit.TestContext.Current.CancellationToken);
        var remainingValue = await context.Cache.GetAsync(
            ProtectedDistributedUserTokenStoreTestContext.GetCacheKey("session-b"),
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Succeeded.ShouldBeFalse();
        remainingValue.ShouldBeNull();
    }

    [Fact]
    public async Task Returns_the_source_refresh_token_for_a_parameterized_token_cache_miss()
    {
        // Arrange
        var context = new ProtectedDistributedUserTokenStoreTestContext();
        var user = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-a");
        await context.Store.StoreTokenAsync(
            user,
            ProtectedDistributedUserTokenStoreTestContext.CreateToken("source-access-token"),
            ct: Xunit.TestContext.Current.CancellationToken);

        // Act
        var result = await context.Store.GetTokenAsync(
            user,
            new UserTokenRequestParameters { Scope = Scope.Parse("admin-api") },
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Succeeded.ShouldBeTrue();
        var tokens = result.Token ?? throw new InvalidOperationException("The refresh token was not retrieved.");
        tokens.TokenForSpecifiedParameters.ShouldBeNull();
        var refreshToken = tokens.RefreshToken ?? throw new InvalidOperationException("The refresh token was not retrieved.");
        refreshToken.RefreshToken.ToString().ShouldBe("refresh-token");
    }

    [Fact]
    public async Task Round_trips_all_supported_user_token_fields()
    {
        // Arrange
        var context = new ProtectedDistributedUserTokenStoreTestContext();
        var user = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-a");
        var expiration = DateTimeOffset.UtcNow.AddMinutes(5);
        var token = new UserToken
        {
            AccessToken = AccessToken.Parse("source-access-token"),
            AccessTokenType = AccessTokenType.Parse("Bearer"),
            ClientId = ClientId.Parse("web-app"),
            Expiration = expiration,
            IdentityToken = IdentityToken.Parse("header.payload.signature"),
            RefreshToken = RefreshToken.Parse("refresh-token"),
            Scope = Scope.Parse("openid offline_access")
        };

        // Act
        await context.Store.StoreTokenAsync(user, token, ct: Xunit.TestContext.Current.CancellationToken);
        var result = await context.Store.GetTokenAsync(user, ct: Xunit.TestContext.Current.CancellationToken);

        // Assert
        var tokens = result.Token ?? throw new InvalidOperationException("The user token was not retrieved.");
        var storedToken = tokens.TokenForSpecifiedParameters ?? throw new InvalidOperationException("The access token was not retrieved.");
        storedToken.AccessToken.ToString().ShouldBe("source-access-token");
        storedToken.AccessTokenType?.ToString().ShouldBe("Bearer");
        storedToken.ClientId.ToString().ShouldBe("web-app");
        storedToken.Expiration.ToUnixTimeMilliseconds().ShouldBe(expiration.ToUnixTimeMilliseconds());
        storedToken.IdentityToken?.ToString().ShouldBe("header.payload.signature");
        storedToken.RefreshToken?.ToString().ShouldBe("refresh-token");
        storedToken.Scope?.ToString().ShouldBe("openid offline_access");
    }

    [Fact]
    public async Task Does_not_return_tokens_after_the_session_expires()
    {
        // Arrange
        var context = new ProtectedDistributedUserTokenStoreTestContext();
        var activeUser = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-a");
        var expiredUser = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-a", DateTimeOffset.UtcNow.AddMinutes(-1));
        await context.Store.StoreTokenAsync(
            activeUser,
            ProtectedDistributedUserTokenStoreTestContext.CreateToken("source-access-token"),
            ct: Xunit.TestContext.Current.CancellationToken);

        // Act
        var result = await context.Store.GetTokenAsync(expiredUser, ct: Xunit.TestContext.Current.CancellationToken);
        var cachedValue = await context.Cache.GetAsync(
            ProtectedDistributedUserTokenStoreTestContext.GetCacheKey("session-a"),
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Succeeded.ShouldBeFalse();
        cachedValue.ShouldBeNull();
    }

    [Fact]
    public async Task Does_not_return_tokens_after_the_session_expires_when_cache_cleanup_fails()
    {
        // Arrange
        var innerCache = new Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(
                new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions()));
        var cache = new ThrowingRemoveDistributedCache(innerCache);
        var context = new ProtectedDistributedUserTokenStoreTestContext(cache);
        var activeUser = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-a");
        var expiredUser = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-a", DateTimeOffset.UtcNow.AddMinutes(-1));
        await context.Store.StoreTokenAsync(
            activeUser,
            ProtectedDistributedUserTokenStoreTestContext.CreateToken("source-access-token"),
            ct: Xunit.TestContext.Current.CancellationToken);

        // Act
        var result = await context.Store.GetTokenAsync(expiredUser, ct: Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Succeeded.ShouldBeFalse();
        cache.RemoveCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Clears_an_expired_token_session_when_cache_cleanup_fails()
    {
        // Arrange
        var innerCache = new Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(
                new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions()));
        var cache = new ThrowingRemoveDistributedCache(innerCache);
        var context = new ProtectedDistributedUserTokenStoreTestContext(cache);
        var activeUser = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-a");
        var expiredUser = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-a", DateTimeOffset.UtcNow.AddMinutes(-1));
        await context.Store.StoreTokenAsync(
            activeUser,
            ProtectedDistributedUserTokenStoreTestContext.CreateToken("source-access-token"),
            ct: Xunit.TestContext.Current.CancellationToken);

        // Act
        await context.Store.ClearTokenAsync(expiredUser, ct: Xunit.TestContext.Current.CancellationToken);

        // Assert
        cache.RemoveCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Treats_corrupted_user_tokens_as_missing_when_cache_cleanup_fails()
    {
        // Arrange
        var innerCache = new Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(
                new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions()));
        var cache = new ThrowingRemoveDistributedCache(innerCache);
        var context = new ProtectedDistributedUserTokenStoreTestContext(cache);
        var user = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-a");
        await context.Store.StoreTokenAsync(
            user,
            ProtectedDistributedUserTokenStoreTestContext.CreateToken("source-access-token"),
            ct: Xunit.TestContext.Current.CancellationToken);
        await context.Cache.SetAsync(
            ProtectedDistributedUserTokenStoreTestContext.GetCacheKey("session-a"),
            [0x01],
            new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions(),
            Xunit.TestContext.Current.CancellationToken);

        // Act
        var result = await context.Store.GetTokenAsync(user, ct: Xunit.TestContext.Current.CancellationToken);

        // Assert
        result.Succeeded.ShouldBeFalse();
        cache.RemoveCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Propagates_cache_failures_when_clearing_an_active_token_session()
    {
        // Arrange
        var innerCache = new Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(
                new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions()));
        var cache = new ThrowingRemoveDistributedCache(innerCache);
        var context = new ProtectedDistributedUserTokenStoreTestContext(cache);
        var user = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-a");
        await context.Store.StoreTokenAsync(
            user,
            ProtectedDistributedUserTokenStoreTestContext.CreateToken("source-access-token"),
            ct: Xunit.TestContext.Current.CancellationToken);

        // Act
        Func<Task> clearToken = () => context.Store.ClearTokenAsync(user, ct: Xunit.TestContext.Current.CancellationToken);

        // Assert
        await clearToken.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public async Task Clears_all_tokens_when_the_session_signs_out()
    {
        // Arrange
        var context = new ProtectedDistributedUserTokenStoreTestContext();
        var user = ProtectedDistributedUserTokenStoreTestContext.CreateUser("session-a");
        await context.Store.StoreTokenAsync(
            user,
            ProtectedDistributedUserTokenStoreTestContext.CreateToken("source-access-token"),
            ct: Xunit.TestContext.Current.CancellationToken);

        // Act
        await context.Store.ClearAll(user, Xunit.TestContext.Current.CancellationToken);
        var cachedValue = await context.Cache.GetAsync(
            ProtectedDistributedUserTokenStoreTestContext.GetCacheKey("session-a"),
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        cachedValue.ShouldBeNull();
    }
}
