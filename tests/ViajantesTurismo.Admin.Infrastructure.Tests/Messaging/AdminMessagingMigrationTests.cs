using Npgsql;
using SharedKernel.Testing;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Messaging;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.MessagingMigrationCapability)]
public sealed class AdminMessagingMigrationTests(PostgreSqlTestServerFixture fixture) : IAsyncLifetime
{
    private AdminMessagingMigrationScenario? scenario;

    private AdminMessagingMigrationScenario Scenario =>
        scenario ?? throw new InvalidOperationException("Test scenario is not initialized.");

    public async ValueTask InitializeAsync()
    {
        scenario = await AdminMessagingMigrationScenario.Create(fixture, TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        var currentScenario = scenario;
        scenario = null;

        if (currentScenario is not null)
        {
            await currentScenario.DisposeAsync();
        }
    }

    [Fact]
    public async Task Removing_the_unused_admin_inbox_refuses_rows_and_rolls_back_the_entire_migration()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await Scenario.ApplyInitialMigration(ct);
        var outboxId = await Scenario.InsertOutboxRow(ct);
        await Scenario.InsertUnexpectedInboxRow(ct);
        var inboxRowBefore = await Scenario.GetInboxRow(ct);
        var outboxRowBefore = await Scenario.GetOutboxRow(outboxId, ct);
        var migrationHistoryBefore = await Scenario.GetMigrationHistory(ct);

        // Act
        Func<Task> migrate = () => Scenario.ApplyRemovalMigration(ct);
        var exception = await migrate.ShouldThrow<PostgresException>();
        var inboxTableExists = await Scenario.InboxTableExists(ct);
        var inboxRowAfter = await Scenario.GetInboxRow(ct);
        var outboxTableExists = await Scenario.OutboxTableExists(ct);
        var outboxRowAfter = await Scenario.GetOutboxRow(outboxId, ct);
        var migrationHistoryAfter = await Scenario.GetMigrationHistory(ct);

        // Assert
        exception.MessageText.ShouldContain("messaging.idempotency_keys", StringComparison.Ordinal);
        inboxTableExists.ShouldBeTrue();
        inboxRowAfter.ShouldBe(inboxRowBefore);
        outboxTableExists.ShouldBeTrue();
        outboxRowAfter.ShouldBe(outboxRowBefore);
        migrationHistoryAfter.ShouldBe(migrationHistoryBefore);
    }

    [Fact]
    public async Task Removing_an_empty_admin_inbox_preserves_the_exact_outbox_row()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await Scenario.ApplyInitialMigration(ct);
        var outboxId = await Scenario.InsertOutboxRow(ct);
        var outboxRowBefore = await Scenario.GetOutboxRow(outboxId, ct);

        // Act
        await Scenario.ApplyRemovalMigration(ct);
        var inboxTableExists = await Scenario.InboxTableExists(ct);
        var outboxTableExists = await Scenario.OutboxTableExists(ct);
        var outboxRowAfter = await Scenario.GetOutboxRow(outboxId, ct);
        var migrationHistory = await Scenario.GetMigrationHistory(ct);

        // Assert
        inboxTableExists.ShouldBeFalse();
        outboxTableExists.ShouldBeTrue();
        outboxRowAfter.ShouldBe(outboxRowBefore);
        migrationHistory.ShouldContain(AdminMessagingMigrationScenario.RemovalMigration);
    }

    [Fact]
    public async Task Concurrent_legacy_insert_cannot_pass_between_the_guard_and_drop()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await Scenario.ApplyInitialMigration(ct);
        var outboxId = await Scenario.InsertOutboxRow(ct);
        var outboxRowBefore = await Scenario.GetOutboxRow(outboxId, ct);

        // Act
        var insertException = await Scenario.ApplyRemovalMigrationWithConcurrentInboxInsert(ct);
        var inboxTableExists = await Scenario.InboxTableExists(ct);
        var outboxRowAfter = await Scenario.GetOutboxRow(outboxId, ct);
        var migrationHistory = await Scenario.GetMigrationHistory(ct);

        // Assert
        insertException.SqlState.ShouldBe(PostgresErrorCodes.UndefinedTable);
        inboxTableExists.ShouldBeFalse();
        outboxRowAfter.ShouldBe(outboxRowBefore);
        migrationHistory.ShouldContain(AdminMessagingMigrationScenario.RemovalMigration);
    }

    [Fact]
    public async Task Down_then_up_recreates_and_removes_only_the_legacy_inbox_schema()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await Scenario.ApplyInitialMigration(ct);
        var outboxId = await Scenario.InsertOutboxRow(ct);
        var outboxRowBefore = await Scenario.GetOutboxRow(outboxId, ct);
        await Scenario.ApplyRemovalMigration(ct);

        // Act
        await Scenario.ApplyInitialMigration(ct);
        var inboxExistsAfterDown = await Scenario.InboxTableExists(ct);
        var inboxRowsAfterDown = await Scenario.InboxRowCount(ct);
        var historyAfterDown = await Scenario.GetMigrationHistory(ct);
        await Scenario.ApplyRemovalMigration(ct);
        var inboxExistsAfterRetry = await Scenario.InboxTableExists(ct);
        var outboxRowAfterRetry = await Scenario.GetOutboxRow(outboxId, ct);
        var historyAfterRetry = await Scenario.GetMigrationHistory(ct);

        // Assert
        inboxExistsAfterDown.ShouldBeTrue();
        inboxRowsAfterDown.ShouldBe(0);
        historyAfterDown.ShouldBe([AdminMessagingMigrationScenario.InitialMigration]);
        inboxExistsAfterRetry.ShouldBeFalse();
        outboxRowAfterRetry.ShouldBe(outboxRowBefore);
        historyAfterRetry.ShouldBe([
            AdminMessagingMigrationScenario.InitialMigration,
            AdminMessagingMigrationScenario.RemovalMigration
        ]);
    }

    [Fact]
    public async Task Restoring_required_admin_idempotency_creates_an_empty_table_and_preserves_the_outbox()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await Scenario.ApplyInitialMigration(ct);
        var outboxId = await Scenario.InsertOutboxRow(ct);
        var outboxRowBefore = await Scenario.GetOutboxRow(outboxId, ct);
        await Scenario.ApplyRemovalMigration(ct);

        // Act
        await Scenario.ApplyLatestMigration(ct);
        var inboxTableExists = await Scenario.InboxTableExists(ct);
        var inboxRowCount = await Scenario.InboxRowCount(ct);
        var outboxRowAfter = await Scenario.GetOutboxRow(outboxId, ct);
        var migrationHistory = await Scenario.GetMigrationHistory(ct);

        // Assert
        inboxTableExists.ShouldBeTrue();
        inboxRowCount.ShouldBe(0);
        outboxRowAfter.ShouldBe(outboxRowBefore);
        migrationHistory.ShouldBe([
            AdminMessagingMigrationScenario.InitialMigration,
            AdminMessagingMigrationScenario.RemovalMigration,
            AdminMessagingMigrationScenario.IdempotencyRestoreMigration
        ]);
    }
}
