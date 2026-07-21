using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Documents;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentStorePostgreSqlTests : IAsyncLifetime
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
    public async Task PurgeExpiredDrafts_deletes_only_expired_unfinalized_documents()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var expired = DocumentDraftInfrastructureTestData.CreateDraft(now.AddDays(-DocumentLimits.DraftRetentionDays - 1));
        var boundaryExpired = DocumentDraftInfrastructureTestData.CreateDraft(now.AddDays(-DocumentLimits.DraftRetentionDays));
        var future = DocumentDraftInfrastructureTestData.CreateDraft(now.AddDays(-DocumentLimits.DraftRetentionDays).AddMinutes(1));
        var current = DocumentDraftInfrastructureTestData.CreateDraft(now);
        var finalized = DocumentDraftInfrastructureTestData.CreateFinalizedDraft(now.AddYears(-1));
        await Scenario.Seed(expired, boundaryExpired, future, current, finalized);

        // Act
        var removedCount = await Scenario.PurgeExpiredDrafts(now, TestContext.Current.CancellationToken);

        // Assert
        removedCount.ShouldBe(2);
        var remaining = await Scenario.GetDocuments(TestContext.Current.CancellationToken);
        remaining.Select(document => document.Id).ShouldContain(future.Id);
        remaining.Select(document => document.Id).ShouldContain(current.Id);
        remaining.Select(document => document.Id).ShouldContain(finalized.Id);
        remaining.Select(document => document.Id).ShouldNotContain(expired.Id);
        remaining.Select(document => document.Id).ShouldNotContain(boundaryExpired.Id);
        var retainedFinalized = remaining.ShouldHaveSingleItem(document => document.Id == finalized.Id);
        retainedFinalized.RetentionExpiresAt.ShouldBeNull();
        remaining.Sum(document => document.Fields.Count).ShouldBe(6);
        var lineageCount = await Scenario.GetLineageCount(TestContext.Current.CancellationToken);
        lineageCount.ShouldBe(3);
        var hasRetentionIndex = await Scenario.HasRetentionIndex(TestContext.Current.CancellationToken);
        hasRetentionIndex.ShouldBeTrue();
    }

    [Fact]
    public async Task PurgeExpiredDrafts_runs_with_the_production_retry_strategy()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var expired = DocumentDraftInfrastructureTestData.CreateDraft(
            now.AddDays(-DocumentLimits.DraftRetentionDays - 1));
        await Scenario.Seed(expired);

        // Act
        var removedCount = await Scenario.PurgeExpiredDraftsWithRetries(
            now,
            TestContext.Current.CancellationToken);

        // Assert
        removedCount.ShouldBe(1);
    }

    [Fact]
    public async Task PurgeExpiredDrafts_rolls_back_revision_deletion_when_lineage_cleanup_fails()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var lineage = DocumentDraftInfrastructureTestData.CreateLineage(
            now.AddDays(-DocumentLimits.DraftRetentionDays - 1));
        var expired = lineage.Revisions.ShouldHaveSingleItem();
        await Scenario.Seed(lineage);
        await Scenario.InstallLineageDeleteFailure(TestContext.Current.CancellationToken);

        // Act
        Func<Task> purge = async () =>
            _ = await Scenario.PurgeExpiredDrafts(now, TestContext.Current.CancellationToken);

        // Assert
        var exception = await purge.ShouldThrow<PostgresException>();
        exception.MessageText.ShouldBe("Injected lineage cleanup failure.");
        var remaining = await Scenario.GetDocuments(TestContext.Current.CancellationToken);
        remaining.Select(document => document.Id).ShouldContain(expired.Id);
        var lineageCount = await Scenario.GetLineageCount(TestContext.Current.CancellationToken);
        lineageCount.ShouldBe(1);
    }

    [Fact]
    public async Task PurgeExpiredDrafts_invalidates_a_stale_surviving_lineage()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var lineage = DocumentDraftInfrastructureTestData.CreateLineage(
            now.AddDays(-DocumentLimits.DraftRetentionDays - 1));
        var expired = lineage.Revisions.ShouldHaveSingleItem();
        var currentResult = lineage.CreateRevision(
            expired.Id,
            DocumentDraftInfrastructureTestData.CreateContent("2"),
            now,
            DocumentDraftInfrastructureTestData.CreateAuditContext());
        currentResult.IsSuccess.ShouldBeTrue();
        lineage.ClearDomainEvents();
        await Scenario.Seed(lineage);

        // Act
        var conflictDetected = await Scenario.HasPurgeInvalidatedStaleLineage(
            currentResult.Value.Id,
            now,
            TestContext.Current.CancellationToken);

        // Assert
        var purgedRevision = await Scenario.GetDocumentById(
            expired.Id,
            TestContext.Current.CancellationToken);
        purgedRevision.ShouldBeNull();
        var survivingRevision = await Scenario.GetDocumentById(
            currentResult.Value.Id,
            TestContext.Current.CancellationToken);
        survivingRevision.ShouldNotBeNull();
        conflictDetected.ShouldBeTrue();
    }

    [Fact]
    public async Task Invalid_branding_logo_uri_materializes_as_missing_snapshot_logo()
    {
        // Arrange
        var document = DocumentDraftInfrastructureTestData.CreateDraft(DateTime.UtcNow);
        await Scenario.Seed(document);
        await Scenario.SetBrandingLogoUri(document.Id, "/\\evil.test/logo.svg", TestContext.Current.CancellationToken);

        // Act
        var documents = await Scenario.GetDocuments(TestContext.Current.CancellationToken);

        // Assert
        documents.ShouldHaveSingleItem().BrandingLogoUri.ShouldBeNull();
    }

    [Fact]
    public async Task GetById_preserves_persisted_template_field_order()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        DocumentField[] fields =
        [
            DocumentField.Create("z-template-first", "Template first", "First value", DocumentPrivacyClassification.Public, false).Value,
            DocumentField.Create("a-template-second", "Template second", "Second value", DocumentPrivacyClassification.Operational, false).Value,
        ];
        var documentResult = DocumentDraft.Create(
            Guid.CreateVersion7(),
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            "tour-service-contract",
            "1",
            "SOURCE-VERSION",
            fields,
            "BRANDING-VERSION",
            "Viajantes Turismo",
            null,
            now);
        documentResult.IsSuccess.ShouldBeTrue();
        var document = documentResult.Value;
        await Scenario.Seed(document);

        // Act
        var reloaded = await Scenario.GetDocumentById(document.Id, TestContext.Current.CancellationToken);

        // Assert
        reloaded.ShouldNotBeNull().ShouldHaveFieldIdsInOrder(["z-template-first", "a-template-second"]);
    }

    [Fact]
    public async Task GetByDocumentId_rehydrates_version_and_revision_high_water_marks()
    {
        // Arrange
        var now = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc);
        var lineage = DocumentDraftInfrastructureTestData.CreateLineage(now);
        var first = lineage.Revisions.ShouldHaveSingleItem();
        var auditContext = DocumentDraftInfrastructureTestData.CreateAuditContext();
        var secondResult = lineage.CreateRevision(
            first.Id,
            DocumentDraftInfrastructureTestData.CreateContent("2"),
            now.AddMinutes(1),
            auditContext);
        secondResult.IsSuccess.ShouldBeTrue();
        var second = secondResult.Value;
        lineage.BeginReview(second.Id, now.AddMinutes(2), auditContext).IsSuccess.ShouldBeTrue();
        lineage.Approve(second.Id, now.AddMinutes(3), auditContext).IsSuccess.ShouldBeTrue();
        lineage.Finalize(second.Id, "artifact"u8.ToArray(), now.AddMinutes(4), auditContext).IsSuccess.ShouldBeTrue();
        lineage.ClearDomainEvents();
        await Scenario.Seed(lineage);

        // Act
        var reloaded = await Scenario.GetLineageByDocumentId(second.Id, TestContext.Current.CancellationToken);
        var reloadedLineage = reloaded.ShouldNotBeNull();
        var thirdResult = reloadedLineage.CreateRevision(
            second.Id,
            DocumentDraftInfrastructureTestData.CreateContent("3"),
            now.AddMinutes(5),
            auditContext);

        // Assert
        reloadedLineage.HighestFinalizedRevision.ShouldBe(2);
        reloadedLineage.HighestRevision.ShouldBe(3);
        reloadedLineage.Version.ShouldBe(5);
        thirdResult.IsSuccess.ShouldBeTrue();
        thirdResult.Value.Revision.ShouldBe(3);
    }

    [Fact]
    public async Task GetAuditMetadataById_returns_only_document_audit_identifiers()
    {
        // Arrange
        var document = DocumentDraftInfrastructureTestData.CreateDraft(DateTime.UtcNow);
        await Scenario.Seed(document);

        // Act
        var metadata = await Scenario.GetAuditMetadataById(document.Id, TestContext.Current.CancellationToken);

        // Assert
        var auditMetadata = metadata.ShouldNotBeNull();
        auditMetadata.BookingId.ShouldBe(document.BookingId);
        auditMetadata.Revision.ShouldBe(document.Revision);
    }

    [Fact]
    public async Task DocumentLineage_rejects_concurrent_revision_updates()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var document = DocumentDraftInfrastructureTestData.CreateDraft(now);
        await Scenario.Seed(document);

        // Act
        var conflictDetected = await Scenario.HasConcurrentLineageUpdateConflict(
            document.Id,
            now,
            TestContext.Current.CancellationToken);

        // Assert
        conflictDetected.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveEntities_translates_duplicate_lineage_constraint()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        var now = new DateTime(2026, 7, 18, 0, 0, 0, DateTimeKind.Utc);
        var first = DocumentDraftInfrastructureTestData.CreateDraft(now, bookingId);
        var second = DocumentDraftInfrastructureTestData.CreateDraft(now.AddMinutes(1), bookingId);

        // Act
        Func<Task> save = async () =>
            await Scenario.SaveDuplicateRevisions(first, second, TestContext.Current.CancellationToken);

        // Assert
        var conflict = await save.ShouldThrow<DocumentRevisionConflictException>();
        var updateException = conflict.InnerException.ShouldBeOfType<DbUpdateException>();
        var postgresException = updateException.InnerException.ShouldBeOfType<PostgresException>();
        postgresException.SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
        postgresException.ConstraintName.ShouldBe("UX_DocumentLineages_BookingId_Type");
    }

    [Fact]
    public async Task SaveEntities_rejects_multiple_active_finalized_revisions_in_one_lineage()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        var now = new DateTime(2026, 7, 18, 0, 0, 0, DateTimeKind.Utc);
        var first = DocumentDraftInfrastructureTestData.CreateFinalizedDraft(now, bookingId);
        var secondResult = first.CreateRevision(
            "tour-service-contract",
            "2",
            Guid.CreateVersion7().ToString("N"),
            first.Fields,
            Guid.CreateVersion7().ToString("N"),
            "Viajantes Turismo",
            new Uri("/logo.svg", UriKind.Relative),
            now.AddMinutes(1));
        secondResult.IsSuccess.ShouldBeTrue();
        var second = secondResult.Value;
        second.BeginReview(now).IsSuccess.ShouldBeTrue();
        second.Approve(now).IsSuccess.ShouldBeTrue();
        second.Finalize("second"u8.ToArray(), now).IsSuccess.ShouldBeTrue();

        // Act
        Func<Task> save = async () =>
            await Scenario.SaveDuplicateRevisions(first, second, TestContext.Current.CancellationToken);

        // Assert
        var conflict = await save.ShouldThrow<DocumentRevisionConflictException>();
        var postgresException = conflict.InnerException.ShouldBeOfType<PostgresException>();
        postgresException.SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
        postgresException.ConstraintName.ShouldBe("UQ_DocumentDrafts_ActiveFinalizedLineage");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(BookingStatus.Pending)]
    public async Task SaveEntities_rejects_documents_for_pending_and_missing_bookings(BookingStatus? bookingStatus)
    {
        // Arrange
        var document = DocumentDraftInfrastructureTestData.CreateDraft(DateTime.UtcNow);

        // Act
        Func<Task> save = async () =>
            await Scenario.SaveDocumentForBookingStatus(document, bookingStatus, TestContext.Current.CancellationToken);

        // Assert
        var conflict = await save.ShouldThrow<DocumentBookingEligibilityConflictException>();
        var updateException = conflict.InnerException.ShouldBeOfType<DbUpdateException>();
        var postgresException = updateException.InnerException.ShouldBeOfType<PostgresException>();
        postgresException.SqlState.ShouldBe(PostgresErrorCodes.CheckViolation);
        postgresException.ConstraintName.ShouldBe("CK_DocumentDrafts_BookingEligibility");
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Completed)]
    public async Task SaveEntities_accepts_documents_for_eligible_bookings(BookingStatus bookingStatus)
    {
        // Arrange
        var document = DocumentDraftInfrastructureTestData.CreateDraft(DateTime.UtcNow);

        // Act
        await Scenario.SaveDocumentForBookingStatus(document, bookingStatus, TestContext.Current.CancellationToken);
        var persisted = await Scenario.GetDocumentById(document.Id, TestContext.Current.CancellationToken);

        // Assert
        persisted.ShouldNotBeNull().Id.ShouldBe(document.Id);
    }
}
