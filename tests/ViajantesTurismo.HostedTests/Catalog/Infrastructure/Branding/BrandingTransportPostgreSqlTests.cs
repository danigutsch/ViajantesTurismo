using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SharedKernel.Testing;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Branding;

/// <summary>Verifies the Branding event payload guard on the migrated transport database.</summary>
[TestScope(TestScope.Integration)]
[Trait(TestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
public sealed class BrandingTransportPostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task Rejects_an_oversized_branding_payload_for_admin_until_rollback()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var dbContext = BrandingTransportPostgreSqlScenario.CreateDbContext(database.ConnectionString);
        await dbContext.Database.MigrateAsync(ct);
        var text = new string('é', 32_767);
        var atLimit = $"\"{text}\"";
        var pastLimit = $"\"{text}x\"";
        const string eventType = "branding.settings.changed";

        // Act
        var accepted = await BrandingTransportPostgreSqlScenario.InsertMessage(dbContext, "admin", eventType, atLimit, ct);
        Func<Task> enqueue = async () =>
        {
            _ = await BrandingTransportPostgreSqlScenario.InsertMessage(dbContext, "admin", eventType, pastLimit, ct);
        };

        // Assert
        accepted.ShouldBe(1);
        var rejected = await enqueue.ShouldThrow<PostgresException>();
        rejected.SqlState.ShouldBe(PostgresErrorCodes.CheckViolation);
        rejected.ConstraintName.ShouldBe("CK_transport_messages_branding_payload_size");

        // Act
        var migrator = dbContext.Database.GetService<IMigrator>();
        var previousMigration = dbContext.Database.GetMigrations()
            .TakeWhile(static migration => !string.Equals(
                migration, "20261118110059_LimitBrandingTransportPayload", StringComparison.Ordinal))
            .Last();
        await migrator.MigrateAsync(previousMigration, ct);
        var acceptedAfterRollback = await BrandingTransportPostgreSqlScenario.InsertMessage(
            dbContext, "admin", eventType, pastLimit, ct);

        // Assert
        acceptedAfterRollback.ShouldBe(1);
    }

    [Theory]
    [InlineData("admin", "admin.other")]
    [InlineData("catalog", "branding.settings.changed")]
    public async Task Other_transport_queues_and_events_can_store_larger_payloads(string consumer, string eventType)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var dbContext = BrandingTransportPostgreSqlScenario.CreateDbContext(database.ConnectionString);
        await dbContext.Database.MigrateAsync(ct);
        var pastLimit = $"\"{new string('x', 65_535)}\"";

        // Act
        var inserted = await BrandingTransportPostgreSqlScenario.InsertMessage(dbContext, consumer, eventType, pastLimit, ct);

        // Assert
        inserted.ShouldBe(1);
    }
}
