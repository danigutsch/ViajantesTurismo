using Microsoft.Extensions.Caching.Distributed;
using Npgsql;
using SharedKernel.Testing;

namespace ViajantesTurismo.HostedTests.Management.Security;

/// <summary>Verifies conditional writes and deletes through independent Management authentication registrations.</summary>
[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
[Trait(TestTraitNames.HostName, TestTraits.AspireHost)]
[Trait(TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
[TestScope(TestScope.Integration)]
public sealed class PostgreSqlUserTokenCacheConcurrencyTests(HostedTestFixture shared)
{
    private const string Key = "management-user-token:synthetic-session";
    private static readonly byte[] Original = [1, 2, 3, 4];
    private static readonly byte[] Refreshed = [1, 2, 3, 5];
    private static readonly byte[] Competing = [1, 2, 3, 6];

    [Fact]
    public async Task Replacement_preserves_absolute_expiry_while_the_runtime_holds_its_only_lock_connection()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlUserTokenCacheConcurrencyScenario.Create(shared, ct);
        await using var host = scenario.CreateHost();
        var expiry = DateTimeOffset.FromUnixTimeSeconds(TimeProvider.System.GetUtcNow().ToUnixTimeSeconds()).AddHours(1);
        var options = new DistributedCacheEntryOptions { AbsoluteExpiration = expiry };
        await host.Cache.SetAsync(Key, Original, options, ct);
        await host.Cache.SetAsync("management-ticket:synthetic-cookie", Competing, options, ct);
        await using var held = await host.SessionLock.Acquire("synthetic-session", ct);

        // Act
        var replaced = await host.TokenCacheConcurrency.TryReplace(Key, Original, Refreshed, expiry, ct);
        var stored = await host.Cache.GetAsync(Key, ct);
        var persistedExpiry = await scenario.ReadExpiry(Key, ct);
        var ticket = await host.Cache.GetAsync("management-ticket:synthetic-cookie", ct);
        Func<Task> alter = async () => await scenario.AlterTableAsRuntime(ct);
        var alterDenied = await alter.ShouldThrow<PostgresException>();
        Func<Task> createTemp = async () => await scenario.CreateTemporaryTableAsRuntime(ct);
        var createTempDenied = await createTemp.ShouldThrow<PostgresException>();

        // Assert
        replaced.ShouldBeTrue();
        stored.ShouldBe(Refreshed);
        persistedExpiry.ShouldBe((expiry, expiry, false));
        ticket.ShouldBe(Competing);
        alterDenied.SqlState.ShouldBe(PostgresErrorCodes.InsufficientPrivilege);
        createTempDenied.SqlState.ShouldBe(PostgresErrorCodes.InsufficientPrivilege);
    }

    [Fact]
    public async Task Stale_write_and_delete_leave_newer_bytes_and_matching_delete_removes_them()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlUserTokenCacheConcurrencyScenario.Create(shared, ct);
        await using var first = scenario.CreateHost();
        await using var other = scenario.CreateHost();
        var expiry = TimeProvider.System.GetUtcNow().AddHours(1);
        var options = new DistributedCacheEntryOptions { AbsoluteExpiration = expiry };
        await first.Cache.SetAsync(Key, Original, options, ct);
        var expected = await first.Cache.GetAsync(Key, ct);
        expected.ShouldNotBeNull();
        await other.Cache.SetAsync(Key, Refreshed, options, ct);

        // Act
        var replaced = await first.TokenCacheConcurrency.TryReplace(Key, expected, Competing, expiry, ct);
        var staleDelete = await first.TokenCacheConcurrency.TryRemove(Key, expected, ct);
        var retained = await first.Cache.GetAsync(Key, ct);
        var matchingDelete = await first.TokenCacheConcurrency.TryRemove(Key, Refreshed, ct);
        var removed = await other.Cache.GetAsync(Key, ct);

        // Assert
        replaced.ShouldBeFalse();
        staleDelete.ShouldBeFalse();
        retained.ShouldBe(Refreshed);
        matchingDelete.ShouldBeTrue();
        removed.ShouldBeNull();
    }

    [Fact]
    public async Task Replacement_requires_a_live_existing_row_and_future_expiry()
    {
        // Arrange
        const string expiredKey = "management-user-token:synthetic-expired";
        const string missingKey = "management-user-token:synthetic-missing";
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlUserTokenCacheConcurrencyScenario.Create(shared, ct);
        await using var host = scenario.CreateHost();
        var now = TimeProvider.System.GetUtcNow();
        var past = now.AddHours(-1);
        var future = now.AddHours(1);
        var options = new DistributedCacheEntryOptions { AbsoluteExpiration = future };
        await host.Cache.SetAsync(Key, Original, options, ct);
        await host.Cache.SetAsync(expiredKey, Original, options, ct);
        await scenario.Expire(expiredKey, ct);

        // Act
        var replacedWithPastExpiry = await host.TokenCacheConcurrency.TryReplace(Key, Original, Refreshed, past, ct);
        var replacedExpiredRow = await host.TokenCacheConcurrency.TryReplace(expiredKey, Original, Refreshed, future, ct);
        var replacedMissingRow = await host.TokenCacheConcurrency.TryReplace(missingKey, Original, Refreshed, future, ct);
        var removedMissingRow = await host.TokenCacheConcurrency.TryRemove(missingKey, Original, ct);
        var retained = await host.Cache.GetAsync(Key, ct);
        var expired = await host.Cache.GetAsync(expiredKey, ct);
        var missing = await host.Cache.GetAsync(missingKey, ct);

        // Assert
        replacedWithPastExpiry.ShouldBeFalse();
        replacedExpiredRow.ShouldBeFalse();
        replacedMissingRow.ShouldBeFalse();
        removedMissingRow.ShouldBeFalse();
        retained.ShouldBe(Original);
        expired.ShouldBeNull();
        missing.ShouldBeNull();
    }

    [Fact]
    public async Task Concurrent_instances_only_replace_the_same_read_once()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlUserTokenCacheConcurrencyScenario.Create(shared, ct);
        await using var first = scenario.CreateHost();
        await using var second = scenario.CreateHost();
        var expiry = TimeProvider.System.GetUtcNow().AddHours(1);
        await first.Cache.SetAsync(Key, Original, new DistributedCacheEntryOptions { AbsoluteExpiration = expiry }, ct);

        // Act
        var replaced = await Task.WhenAll(
            first.TokenCacheConcurrency.TryReplace(Key, Original, Refreshed, expiry, ct),
            second.TokenCacheConcurrency.TryReplace(Key, Original, Competing, expiry, ct));
        var stored = await second.Cache.GetAsync(Key, ct);

        // Assert
        replaced.Count(static success => success).ShouldBe(1);
        stored.ShouldBe(replaced[0] ? Refreshed : Competing);
    }

    [Fact]
    public async Task Removes_an_existing_corrupt_entry_with_empty_protected_bytes()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlUserTokenCacheConcurrencyScenario.Create(shared, ct);
        await using var host = scenario.CreateHost();
        var expiry = TimeProvider.System.GetUtcNow().AddHours(1);
        await host.Cache.SetAsync(Key, [], new DistributedCacheEntryOptions { AbsoluteExpiration = expiry }, ct);

        // Act
        var removed = await host.TokenCacheConcurrency.TryRemove(Key, ReadOnlyMemory<byte>.Empty, ct);
        var remaining = await host.Cache.GetAsync(Key, ct);

        // Assert
        removed.ShouldBeTrue();
        remaining.ShouldBeNull();
    }
}
