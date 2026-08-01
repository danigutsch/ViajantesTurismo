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
        if (scenario is not null)
        {
            await scenario.DisposeAsync();
        }
    }

    [Fact]
    public async Task Fresh_initial_migration_creates_removes_and_recreates_the_required_schema()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        await Scenario.ApplyInitialMigration(ct);
        await Scenario.InsertIdempotencyRow(ct);
        var idempotencyExists = await Scenario.IdempotencyTableExists(ct);
        var outboxExists = await Scenario.OutboxTableExists(ct);
        var extensionExists = await Scenario.CustomerEmailExtensionExists(ct);
        var idempotencyRows = await Scenario.IdempotencyRowCount(ct);
        var migrationHistory = await Scenario.GetMigrationHistory(ct);
        await Scenario.RemoveAllMigrations(ct);
        var idempotencyExistsAfterDown = await Scenario.IdempotencyTableExists(ct);
        var outboxExistsAfterDown = await Scenario.OutboxTableExists(ct);
        var extensionExistsAfterDown = await Scenario.CustomerEmailExtensionExists(ct);
        await Scenario.ApplyInitialMigration(ct);
        var idempotencyExistsAfterReapply = await Scenario.IdempotencyTableExists(ct);
        var extensionExistsAfterReapply = await Scenario.CustomerEmailExtensionExists(ct);

        // Assert
        idempotencyExists.ShouldBeTrue();
        outboxExists.ShouldBeTrue();
        extensionExists.ShouldBeTrue();
        idempotencyRows.ShouldBe(1);
        migrationHistory.ShouldBe([AdminMessagingMigrationScenario.InitialMigration]);
        idempotencyExistsAfterDown.ShouldBeFalse();
        outboxExistsAfterDown.ShouldBeFalse();
        extensionExistsAfterDown.ShouldBeTrue();
        idempotencyExistsAfterReapply.ShouldBeTrue();
        extensionExistsAfterReapply.ShouldBeTrue();
    }
}
