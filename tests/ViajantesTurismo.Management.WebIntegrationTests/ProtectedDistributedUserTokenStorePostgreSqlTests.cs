namespace ViajantesTurismo.Management.WebIntegrationTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.SecurityCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.DatabaseIntegrationScope)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.AspireHost)]
[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
public sealed class ProtectedDistributedUserTokenStorePostgreSqlTests : IAsyncLifetime
{
    private readonly PostgreSqlManagementUserTokenStoreScenario _scenario = new();

    public ValueTask InitializeAsync()
    {
        return _scenario.InitializeAsync();
    }

    public ValueTask DisposeAsync()
    {
        return _scenario.DisposeAsync();
    }

    [Fact]
    public async Task Waiting_for_an_advisory_lock_propagates_cancellation()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        Func<Task> waitForLock = () => _scenario.WaitForWaitingAdvisoryLock(cancellation.Token);
        var exception = await waitForLock.ShouldThrowAssignableTo<OperationCanceledException>();

        // Assert
        exception.ShouldNotBeNull();
    }

    [Fact]
    public async Task Concurrent_parameterized_stores_for_one_session_preserve_both_tokens()
    {
        // Arrange
        var cache = _scenario.BlockingCache;
        var firstStore = _scenario.CreateStore(cache);
        var secondStore = _scenario.CreateStore(cache);
        var user = PostgreSqlManagementUserTokenStoreScenario.CreateUser("concurrent-session");
        var firstParameters = new UserTokenRequestParameters { Scope = Scope.Parse("admin-api") };
        var secondParameters = new UserTokenRequestParameters { Scope = Scope.Parse("catalog-api") };

        // Act
        var firstStoreTask = firstStore.StoreTokenAsync(
            user,
            PostgreSqlManagementUserTokenStoreScenario.CreateToken("admin-access-token"),
            firstParameters,
            TestContext.Current.CancellationToken);
        await cache.WaitForFirstSet(TestContext.Current.CancellationToken);
        var secondStoreTask = secondStore.StoreTokenAsync(
            user,
            PostgreSqlManagementUserTokenStoreScenario.CreateToken("catalog-access-token"),
            secondParameters,
            TestContext.Current.CancellationToken);

        await _scenario.ReleaseFirstSetAfterWaitingForLock(cache, TestContext.Current.CancellationToken);

        await Task.WhenAll(firstStoreTask, secondStoreTask);
        var firstResult = await firstStore.GetTokenAsync(user, firstParameters, TestContext.Current.CancellationToken);
        var secondResult = await secondStore.GetTokenAsync(user, secondParameters, TestContext.Current.CancellationToken);

        // Assert
        var firstTokens = firstResult.Token ?? throw new InvalidOperationException("The first user token was not retrieved.");
        var firstToken = firstTokens.TokenForSpecifiedParameters ?? throw new InvalidOperationException("The first access token was not retrieved.");
        var secondTokens = secondResult.Token ?? throw new InvalidOperationException("The second user token was not retrieved.");
        var secondToken = secondTokens.TokenForSpecifiedParameters ?? throw new InvalidOperationException("The second access token was not retrieved.");
        firstToken.AccessToken.ToString().ShouldBe("admin-access-token");
        secondToken.AccessToken.ToString().ShouldBe("catalog-access-token");
    }

    [Fact]
    public async Task Concurrent_store_then_clear_for_one_session_leaves_no_token()
    {
        // Arrange
        var cache = _scenario.BlockingCache;
        var firstStore = _scenario.CreateStore(cache);
        var secondStore = _scenario.CreateStore(cache);
        var user = PostgreSqlManagementUserTokenStoreScenario.CreateUser("concurrent-clear-session");
        var parameters = new UserTokenRequestParameters { Scope = Scope.Parse("admin-api") };

        // Act
        var storeToken = firstStore.StoreTokenAsync(
            user,
            PostgreSqlManagementUserTokenStoreScenario.CreateToken("admin-access-token"),
            parameters,
            TestContext.Current.CancellationToken);
        await cache.WaitForFirstSet(TestContext.Current.CancellationToken);
        var clearToken = secondStore.ClearTokenAsync(user, parameters, TestContext.Current.CancellationToken);
        await _scenario.ReleaseFirstSetAfterWaitingForLock(cache, TestContext.Current.CancellationToken);
        await Task.WhenAll(storeToken, clearToken);
        var result = await firstStore.GetTokenAsync(user, parameters, TestContext.Current.CancellationToken);

        // Assert
        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task Concurrent_store_then_sign_out_for_one_session_removes_all_tokens()
    {
        // Arrange
        var cache = _scenario.BlockingCache;
        var firstStore = _scenario.CreateStore(cache);
        var secondStore = _scenario.CreateStore(cache);
        var user = PostgreSqlManagementUserTokenStoreScenario.CreateUser("concurrent-sign-out-session");

        // Act
        var storeToken = firstStore.StoreTokenAsync(
            user,
            PostgreSqlManagementUserTokenStoreScenario.CreateToken("source-access-token"),
            ct: TestContext.Current.CancellationToken);
        await cache.WaitForFirstSet(TestContext.Current.CancellationToken);
        var signOut = secondStore.ClearAll(user, TestContext.Current.CancellationToken);
        await _scenario.ReleaseFirstSetAfterWaitingForLock(cache, TestContext.Current.CancellationToken);
        await Task.WhenAll(storeToken, signOut);
        var result = await firstStore.GetTokenAsync(user, ct: TestContext.Current.CancellationToken);

        // Assert
        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task Does_not_remove_a_new_entry_after_reading_a_corrupt_stale_entry()
    {
        // Arrange
        const string sessionId = "corrupt-stale-session";
        var user = PostgreSqlManagementUserTokenStoreScenario.CreateUser(sessionId);
        await _scenario.SetCorruptEntry(sessionId, TestContext.Current.CancellationToken);
        var blockingCache = _scenario.CreateBlockingFirstGetCache(sessionId);
        var staleStore = _scenario.CreateStore(blockingCache);
        var replacementStore = _scenario.CreateStore();

        // Act
        var staleRead = staleStore.GetTokenAsync(user, ct: TestContext.Current.CancellationToken);
        await blockingCache.WaitForFirstGet(TestContext.Current.CancellationToken);
        await blockingCache.CompleteThenRelease(replacementStore.StoreTokenAsync(
            user,
            PostgreSqlManagementUserTokenStoreScenario.CreateToken("replacement-access-token"),
            ct: TestContext.Current.CancellationToken));
        var staleResult = await staleRead;
        var replacementResult = await replacementStore.GetTokenAsync(user, ct: TestContext.Current.CancellationToken);

        // Assert
        staleResult.Succeeded.ShouldBeFalse();
        replacementResult.Succeeded.ShouldBeTrue();
        var replacementTokens = replacementResult.Token ?? throw new InvalidOperationException("The replacement user token was not retrieved.");
        var replacementToken = replacementTokens.TokenForSpecifiedParameters ?? throw new InvalidOperationException("The replacement access token was not retrieved.");
        replacementToken.AccessToken.ToString().ShouldBe("replacement-access-token");
    }

    [Fact]
    public async Task Signing_out_rejects_a_store_that_was_waiting_for_the_same_session_lock()
    {
        // Arrange
        var cache = _scenario.CreateBlockingFirstRemoveCache();
        var signingOutStore = _scenario.CreateStore(cache);
        var waitingStore = _scenario.CreateStore();
        var user = PostgreSqlManagementUserTokenStoreScenario.CreateUser("sign-out-race-session");

        // Act
        var signOut = signingOutStore.ClearAll(user, TestContext.Current.CancellationToken);
        await cache.WaitForFirstRemove(TestContext.Current.CancellationToken);
        var storeToken = waitingStore.StoreTokenAsync(
            user,
            PostgreSqlManagementUserTokenStoreScenario.CreateToken("late-access-token"),
            ct: TestContext.Current.CancellationToken);
        await _scenario.ReleaseFirstRemoveAfterWaitingForLock(cache, signOut, TestContext.Current.CancellationToken);
        Func<Task> awaitStoreToken = () => storeToken;
        var exception = await awaitStoreToken.ShouldThrow<InvalidOperationException>();
        var result = await waitingStore.GetTokenAsync(user, ct: TestContext.Current.CancellationToken);

        // Assert
        exception.Message.ShouldBe("The management token session has been revoked.");
        result.Succeeded.ShouldBeFalse();
    }
}
