using Npgsql;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Domain.Documents;

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
    public async Task Audit_table_contains_only_approved_metadata_columns()
    {
        // Arrange
        string[] expectedColumns =
        [
            "ActorId",
            "BookingId",
            "CorrelationId",
            "DocumentId",
            "DocumentRevision",
            "Id",
            "OccurredAtUtc",
            "Operation",
            "Outcome",
            "ReasonCode",
            "RetentionExpiresAt",
        ];

        // Act
        var columns = await Scenario.GetDocumentAuditColumnNames(TestContext.Current.CancellationToken);

        // Assert
        columns.ShouldBe(expectedColumns);
    }

    [Fact]
    public async Task Audit_records_reject_update_and_delete_before_retention_expires()
    {
        // Arrange
        var record = DocumentAuditInfrastructureTestData.CreateRecord(DateTime.UtcNow);
        await Scenario.SeedAudit(record);

        // Act
        Func<Task> update = async () => await Scenario.UpdateAuditActor(record.Id, "different-actor", TestContext.Current.CancellationToken);
        Func<Task> delete = async () => await Scenario.DeleteAuditRecord(record.Id, TestContext.Current.CancellationToken);

        // Assert
        _ = await update.ShouldThrow<PostgresException>();
        _ = await delete.ShouldThrow<PostgresException>();
    }

    [Fact]
    public async Task Expired_audit_records_still_reject_update()
    {
        // Arrange
        var record = DocumentAuditInfrastructureTestData.CreateRecord(DateTime.UtcNow.AddMonths(-25));
        await Scenario.SeedAudit(record);

        // Act
        Func<Task> update = async () =>
            await Scenario.UpdateAuditActor(record.Id, "different-actor", TestContext.Current.CancellationToken);

        // Assert
        _ = await update.ShouldThrow<PostgresException>();
        var persisted = await Scenario.GetAuditRecord(record.Id, TestContext.Current.CancellationToken);
        persisted.ShouldNotBeNull().ActorId.ShouldBe(record.ActorId);
    }

    [Fact]
    public async Task Audit_records_reject_truncate()
    {
        // Arrange
        var record = DocumentAuditInfrastructureTestData.CreateRecord(DateTime.UtcNow);
        await Scenario.SeedAudit(record);

        // Act
        Func<Task> truncate = async () => await Scenario.TruncateAuditRecords(TestContext.Current.CancellationToken);

        // Assert
        _ = await truncate.ShouldThrow<PostgresException>();
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

    [Fact]
    public async Task PurgeExpiredRecords_deletes_the_boundary_in_bounded_batches_and_then_becomes_a_no_op()
    {
        // Arrange
        var now = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var earlier = Enumerable.Range(0, 500)
            .Select(index => DocumentAuditInfrastructureTestData.CreateRecord(
                now.AddMonths(-DocumentAuditLimits.RetentionMonths - 1).AddMinutes(index)))
            .ToArray();
        var boundary = DocumentAuditInfrastructureTestData.CreateRecord(
            now.AddMonths(-DocumentAuditLimits.RetentionMonths));
        var future = DocumentAuditInfrastructureTestData.CreateRecord(
            now.AddMonths(-DocumentAuditLimits.RetentionMonths).AddDays(1));
        await Scenario.SeedAudits([.. earlier, boundary, future]);

        // Act
        var firstRemovedCount = await Scenario.PurgeExpiredAuditRecords(
            now,
            TestContext.Current.CancellationToken);
        var boundaryAfterFirstBatch = await Scenario.GetAuditRecord(
            boundary.Id,
            TestContext.Current.CancellationToken);
        var futureAfterFirstBatch = await Scenario.GetAuditRecord(
            future.Id,
            TestContext.Current.CancellationToken);
        var secondRemovedCount = await Scenario.PurgeExpiredAuditRecords(
            now,
            TestContext.Current.CancellationToken);
        var boundaryAfterSecondBatch = await Scenario.GetAuditRecord(
            boundary.Id,
            TestContext.Current.CancellationToken);
        var futureAfterSecondBatch = await Scenario.GetAuditRecord(
            future.Id,
            TestContext.Current.CancellationToken);
        var thirdRemovedCount = await Scenario.PurgeExpiredAuditRecords(
            now,
            TestContext.Current.CancellationToken);

        // Assert
        boundary.RetentionExpiresAt.ShouldBe(now);
        firstRemovedCount.ShouldBe(500);
        boundaryAfterFirstBatch.ShouldNotBeNull();
        futureAfterFirstBatch.ShouldNotBeNull();
        secondRemovedCount.ShouldBe(1);
        boundaryAfterSecondBatch.ShouldBeNull();
        futureAfterSecondBatch.ShouldNotBeNull();
        thirdRemovedCount.ShouldBe(0);
    }

    [Fact]
    public async Task Audit_record_survives_document_lineage_deletion()
    {
        // Arrange
        var now = DateTime.UtcNow;
        now = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond));
        var document = DocumentDraftInfrastructureTestData.CreateDraft(
            now.AddDays(-DocumentLimits.DraftRetentionDays - 1));
        var auditResult = DocumentAuditRecord.Create(
            "9c5ff2e6-8b35-4f78-9df3-ef15af8e92a4",
            document.Id,
            document.BookingId,
            document.Revision,
            DocumentAuditOperation.Generate,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.ManualOperation,
            "9a3ca841b4354928861c660a6e4e1b99",
            now);
        auditResult.IsSuccess.ShouldBeTrue();
        var audit = auditResult.Value;
        await Scenario.Seed(document);
        await Scenario.SeedAudit(audit);

        // Act
        var removedCount = await Scenario.PurgeExpiredDrafts(
            now,
            TestContext.Current.CancellationToken);

        // Assert
        removedCount.ShouldBe(1);
        var persistedDocument = await Scenario.GetDocumentById(
            document.Id,
            TestContext.Current.CancellationToken);
        persistedDocument.ShouldBeNull();
        var lineageCount = await Scenario.GetLineageCount(TestContext.Current.CancellationToken);
        lineageCount.ShouldBe(0);
        var persistedAudit = await Scenario.GetAuditRecord(
            audit.Id,
            TestContext.Current.CancellationToken);
        var retainedAudit = persistedAudit.ShouldNotBeNull();
        retainedAudit.DocumentId.ShouldBe(document.Id);
        retainedAudit.BookingId.ShouldBe(document.BookingId);
        retainedAudit.DocumentRevision.ShouldBe(document.Revision);
        retainedAudit.Operation.ShouldBe(DocumentAuditOperation.Generate);
        retainedAudit.Outcome.ShouldBe(DocumentAuditOutcome.Succeeded);
        retainedAudit.ReasonCode.ShouldBe(DocumentAuditReasonCode.ManualOperation);
        retainedAudit.RetentionExpiresAt.ShouldBe(audit.RetentionExpiresAt);
    }

    [Fact]
    public async Task Reset_removes_mutable_document_data_and_preserves_audit_records()
    {
        // Arrange
        var now = new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc);
        var document = DocumentDraftInfrastructureTestData.CreateDraft(now);
        var audit = DocumentAuditInfrastructureTestData.CreateRecord(
            now,
            document.Id,
            document.BookingId,
            document.Revision);
        await Scenario.Seed(document);
        await Scenario.SeedAudit(audit);

        // Act
        await Scenario.ResetMutableDataPreservingDocumentAudits(TestContext.Current.CancellationToken);
        var remainingDocuments = await Scenario.GetDocuments(TestContext.Current.CancellationToken);
        var remainingLineageCount = await Scenario.GetLineageCount(TestContext.Current.CancellationToken);
        var persistedAudit = await Scenario.GetAuditRecord(audit.Id, TestContext.Current.CancellationToken);

        // Assert
        remainingDocuments.ShouldBeEmpty();
        remainingLineageCount.ShouldBe(0);
        var retainedAudit = persistedAudit.ShouldNotBeNull();
        retainedAudit.ActorId.ShouldBe(audit.ActorId);
        retainedAudit.DocumentId.ShouldBe(document.Id);
        retainedAudit.BookingId.ShouldBe(document.BookingId);
    }
}
