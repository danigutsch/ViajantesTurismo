using Npgsql;
using SharedKernel.Testing;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Documents;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentAuditStorePostgreSqlTests : IAsyncLifetime
{
    private PostgreSqlDocumentStoreScenario? scenario;

    private PostgreSqlDocumentStoreScenario Scenario =>
        scenario ?? throw new InvalidOperationException("Test scenario is not initialized.");

    public async ValueTask InitializeAsync()
    {
        scenario = await PostgreSqlDocumentStoreScenario.Create(TestContext.Current.CancellationToken);
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
    public async Task Add_persists_only_auditable_metadata_with_24_month_retention()
    {
        // Arrange
        var occurredAt = new DateTime(2026, 7, 16, 9, 0, 0, DateTimeKind.Utc);
        var record = DocumentAuditInfrastructureTestData.CreateRecord(occurredAt);

        // Act
        await Scenario.SeedAudit(record);
        var persisted = await Scenario.GetAuditRecord(record.Id, TestContext.Current.CancellationToken);

        // Assert
        var audit = persisted.ShouldNotBeNull();
        audit.ActorId.ShouldBe(record.ActorId);
        audit.DocumentId.ShouldBe(record.DocumentId);
        audit.BookingId.ShouldBe(record.BookingId);
        audit.DocumentRevision.ShouldBe(record.DocumentRevision);
        audit.Operation.ShouldBe(record.Operation);
        audit.Outcome.ShouldBe(record.Outcome);
        audit.ReasonCode.ShouldBe(record.ReasonCode);
        audit.CorrelationId.ShouldBe(record.CorrelationId);
        audit.RetentionExpiresAt.ShouldBe(occurredAt.AddMonths(24));
    }

    [Fact]
    public async Task Audit_records_reject_update_and_delete_before_retention_expires()
    {
        // Arrange
        var record = DocumentAuditInfrastructureTestData.CreateRecord(new DateTime(2026, 7, 16, 9, 0, 0, DateTimeKind.Utc));
        await Scenario.SeedAudit(record);

        // Act
        Func<Task> update = async () => await Scenario.UpdateAuditActor(record.Id, "different-actor", TestContext.Current.CancellationToken);
        Func<Task> delete = async () => await Scenario.DeleteAuditRecord(record.Id, TestContext.Current.CancellationToken);

        // Assert
        _ = await update.ShouldThrow<PostgresException>();
        _ = await delete.ShouldThrow<PostgresException>();
    }

    [Fact]
    public async Task PurgeExpiredRecords_deletes_only_records_after_their_retention_period()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var expired = DocumentAuditInfrastructureTestData.CreateRecord(now.AddMonths(-25));
        var current = DocumentAuditInfrastructureTestData.CreateRecord(now);
        await Scenario.SeedAudit(expired);
        await Scenario.SeedAudit(current);

        // Act
        var removedCount = await Scenario.PurgeExpiredAuditRecords(now, TestContext.Current.CancellationToken);
        var expiredRecord = await Scenario.GetAuditRecord(expired.Id, TestContext.Current.CancellationToken);
        var currentRecord = await Scenario.GetAuditRecord(current.Id, TestContext.Current.CancellationToken);

        // Assert
        removedCount.ShouldBe(1);
        expiredRecord.ShouldBeNull();
        currentRecord.ShouldNotBeNull();
    }
}
