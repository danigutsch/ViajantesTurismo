using Npgsql;
using PostgreSqlTestDatabaseLease = PandaBear.Npgsql.Testing.PostgreSqlTestDatabaseLease;
using PandaBear.Testing;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.HostedTests.Management.Security;

/// <summary>
/// Verifies that the Management Web session lock serializes one session's token refresh, exchange and sign-out across
/// instances, so a rotated refresh token is never redeemed twice.
/// </summary>
[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
[Trait(TestTraitNames.HostName, TestTraits.AspireHost)]
[Trait(TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
[TestScope(TestScope.Integration)]
public sealed class PostgreSqlUserSessionLockTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>, IAsyncLifetime
{
    private static readonly TimeSpan WaitBudget = TimeSpan.FromSeconds(10);

    private PostgreSqlTestDatabaseLease? database;
    private NpgsqlDataSource? firstInstance;
    private NpgsqlDataSource? secondInstance;

    private NpgsqlDataSource FirstInstance => firstInstance ?? throw new InvalidOperationException("The test is not initialized.");

    private NpgsqlDataSource SecondInstance => secondInstance ?? throw new InvalidOperationException("The test is not initialized.");

    public async ValueTask InitializeAsync()
    {
        database = await fixture.CreateDatabase(TestContext.Current.CancellationToken);
        firstInstance = database.CreateDataSource("first-instance");
        secondInstance = database.CreateDataSource("second-instance");
    }

    public async ValueTask DisposeAsync()
    {
        if (firstInstance is not null)
        {
            await firstInstance.DisposeAsync();
        }

        if (secondInstance is not null)
        {
            await secondInstance.DisposeAsync();
        }

        if (database is not null)
        {
            await database.DisposeAsync();
        }
    }

    [Fact]
    public async Task A_second_instance_waits_until_the_first_releases_the_session()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var first = new PostgreSqlUserSessionLock(FirstInstance);
        var second = new PostgreSqlUserSessionLock(SecondInstance);
        var held = await first.Acquire("session-a", ct);
        var waiting = second.Acquire("session-a", ct).AsTask();
        await PostgreSqlAdvisoryLockProbe.WaitForBlockedLock(FirstInstance, WaitBudget, ct);
        var completedWhileHeld = waiting.IsCompleted;

        // Act
        await held.DisposeAsync();
        await using var acquired = await waiting.WaitAsync(WaitBudget, ct);

        // Assert
        completedWhileHeld.ShouldBeFalse();
        acquired.ShouldNotBeNull();
    }

    [Fact]
    public async Task Different_sessions_do_not_wait_for_each_other()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var first = new PostgreSqlUserSessionLock(FirstInstance);
        var second = new PostgreSqlUserSessionLock(SecondInstance);
        await using var held = await first.Acquire("session-a", ct);

        // Act
        await using var other = await second.Acquire("session-b", ct).AsTask().WaitAsync(WaitBudget, ct);

        // Assert
        other.ShouldNotBeNull();
    }

    [Fact]
    public async Task A_cancelled_wait_gives_up_without_taking_the_session()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var first = new PostgreSqlUserSessionLock(FirstInstance);
        var second = new PostgreSqlUserSessionLock(SecondInstance);
        var held = await first.Acquire("session-a", ct);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var waiting = second.Acquire("session-a", cancellation.Token).AsTask();
        await PostgreSqlAdvisoryLockProbe.WaitForBlockedLock(FirstInstance, WaitBudget, ct);

        // Act
        await cancellation.CancelAsync();
        Func<Task> wait = () => waiting;
        _ = await wait.ShouldThrowAssignableTo<OperationCanceledException>();
        await held.DisposeAsync();
        await using var next = await first.Acquire("session-a", ct).AsTask().WaitAsync(WaitBudget, ct);

        // Assert
        next.ShouldNotBeNull();
    }

    [Fact]
    public async Task Releasing_twice_releases_once()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var sessionLock = new PostgreSqlUserSessionLock(FirstInstance);
        var held = await sessionLock.Acquire("session-a", ct);

        // Act
        await held.DisposeAsync();
        await held.DisposeAsync();
        await using var next = await new PostgreSqlUserSessionLock(SecondInstance).Acquire("session-a", ct).AsTask().WaitAsync(WaitBudget, ct);

        // Assert
        next.ShouldNotBeNull();
    }
}
