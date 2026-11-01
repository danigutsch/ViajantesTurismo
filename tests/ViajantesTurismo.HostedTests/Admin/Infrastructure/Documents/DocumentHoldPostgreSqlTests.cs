using PandaBear.DispositionHold;
using PandaBear.Results;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Documents;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, ViajantesTurismo.Admin.Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
[TestScope(TestScope.Integration)]
public sealed class DocumentHoldPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IAsyncLifetime, IClassFixture<PostgreSqlTestServerFixture>
{
    private PostgreSqlDocumentStoreScenario? scenario;
    private DocumentHoldPostgreSqlHarness? harness;

    private PostgreSqlDocumentStoreScenario Scenario =>
        scenario ?? throw new InvalidOperationException("Test scenario is not initialized.");

    private DocumentHoldPostgreSqlHarness Harness =>
        harness ?? throw new InvalidOperationException("Test harness is not initialized.");

    public async ValueTask InitializeAsync()
    {
        scenario = await PostgreSqlDocumentStoreScenario.Create(fixture, TestContext.Current.CancellationToken);
        harness = DocumentHoldPostgreSqlHarness.Create(scenario.ConnectionString);
    }

    public async ValueTask DisposeAsync()
    {
        if (harness is not null)
        {
            await harness.DisposeAsync();
        }

        if (scenario is not null)
        {
            await scenario.DisposeAsync();
        }
    }

    [Fact]
    public async Task A_held_lineage_is_skipped_by_the_draft_purge_until_the_hold_is_released()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var created = TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays - 1);
        var lineage = DocumentDraftInfrastructureTestData.CreateLineage(created);
        var documentId = lineage.Revisions.ShouldHaveSingleItem().Id;
        await Scenario.Seed(lineage);
        var holdId = await Harness.Place(documentId, ct);

        // Act
        var whileHeld = await Harness.PurgeDrafts(ct);
        var documentsWhileHeld = await Scenario.GetDocuments(ct);
        var remainingWhileHeld = documentsWhileHeld.Length;
        await Harness.Release(documentId, holdId, ct);
        var afterRelease = await Harness.PurgeDrafts(ct);

        // Assert
        whileHeld.ShouldBe(new DocumentPurgeCounts(0, 1));
        remainingWhileHeld.ShouldBe(1);
        afterRelease.ShouldBe(new DocumentPurgeCounts(1, 0));
        var remainingAfterRelease = await Scenario.GetDocuments(ct);
        remainingAfterRelease.ShouldBeEmpty();
    }

    [Fact]
    public async Task Draft_purge_persists_evidence_with_the_held_count_and_then_the_purged_count()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var created = TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays - 1);
        var lineage = DocumentDraftInfrastructureTestData.CreateLineage(created);
        var documentId = lineage.Revisions.ShouldHaveSingleItem().Id;
        await Scenario.Seed(lineage);
        var holdId = await Harness.Place(documentId, ct);

        // Act
        _ = await Harness.PurgeDrafts(ct);
        await Harness.Release(documentId, holdId, ct);
        _ = await Harness.PurgeDrafts(ct);

        // Assert
        var evidence = await Scenario.GetRetentionPurgeEvidence(ct);
        evidence.Length.ShouldBe(2);
        evidence.Select(record => record.Kind).Distinct().ShouldBe([DocumentRetentionPurgeKind.Drafts]);
        evidence.Select(record => record.Outcome).Distinct().ShouldBe([DocumentRetentionPurgeOutcome.Succeeded]);
        evidence.Select(record => record.PurgedCount).ShouldBe([0, 1]);
        evidence.Select(record => record.HeldCount).ShouldBe([1, 0]);
    }

    [Fact]
    public async Task Idempotency_purge_persists_evidence_with_the_held_and_purged_counts()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var now = TimeProvider.System.GetUtcNow();
        var lineage = DocumentDraftInfrastructureTestData.CreateLineage(now.UtcDateTime);
        var documentId = lineage.Revisions.ShouldHaveSingleItem().Id;
        await Scenario.Seed(lineage);
        _ = await Harness.Place(documentId, ct);
        await Scenario.SeedIdempotencyEntry($"admin.documents.regenerate-draft:{documentId:N}", "held-1", now.AddDays(-40), now.AddDays(-39), ct);
        await Scenario.SeedIdempotencyEntry("admin.documents.generate-contract-draft:booking", "free-1", now.AddDays(-40), now.AddDays(-39), ct);

        // Act
        var counts = await Harness.PurgeIdempotencyEntries(ct);

        // Assert
        counts.ShouldBe(new DocumentPurgeCounts(1, 1));
        var allEvidence = await Scenario.GetRetentionPurgeEvidence(ct);
        var record = allEvidence.ShouldHaveSingleItem();
        record.Kind.ShouldBe(DocumentRetentionPurgeKind.IdempotencyEntries);
        record.Outcome.ShouldBe(DocumentRetentionPurgeOutcome.Succeeded);
        record.PurgedCount.ShouldBe(1);
        record.HeldCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_refused_draft_purge_deletes_nothing_and_persists_refused_evidence()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var created = TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays - 1);
        await Scenario.Seed(DocumentDraftInfrastructureTestData.CreateLineage(created));
        await using var refusingHarness = DocumentHoldPostgreSqlHarness.Create(
            Scenario.ConnectionString, holdGuardFailure: new DispositionHoldStateUnknownException());

        // Act
        Func<Task> purge = () => refusingHarness.PurgeDrafts(ct);

        // Assert
        await purge.ShouldThrow<DispositionHoldStateUnknownException>();
        var allEvidence = await Scenario.GetRetentionPurgeEvidence(ct);
        var evidence = allEvidence.ShouldHaveSingleItem();
        evidence.Kind.ShouldBe(DocumentRetentionPurgeKind.Drafts);
        evidence.Outcome.ShouldBe(DocumentRetentionPurgeOutcome.Refused);
        evidence.PurgedCount.ShouldBe(0);
        evidence.HeldCount.ShouldBe(0);
        var remaining = await Scenario.GetDocuments(ct);
        remaining.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task A_refused_idempotency_purge_deletes_nothing_and_persists_refused_evidence()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var now = TimeProvider.System.GetUtcNow();
        var lineage = DocumentDraftInfrastructureTestData.CreateLineage(now.UtcDateTime);
        var documentId = lineage.Revisions.ShouldHaveSingleItem().Id;
        await Scenario.Seed(lineage);
        await Scenario.SeedIdempotencyEntry($"admin.documents.regenerate-draft:{documentId:N}", "free-1", now.AddDays(-40), now.AddDays(-39), ct);
        await using var refusingHarness = DocumentHoldPostgreSqlHarness.Create(
            Scenario.ConnectionString, holdGuardFailure: new DispositionHoldStateUnknownException());

        // Act
        Func<Task> purge = () => refusingHarness.PurgeIdempotencyEntries(ct);

        // Assert
        await purge.ShouldThrow<DispositionHoldStateUnknownException>();
        var allEvidence = await Scenario.GetRetentionPurgeEvidence(ct);
        var evidence = allEvidence.ShouldHaveSingleItem();
        evidence.Kind.ShouldBe(DocumentRetentionPurgeKind.IdempotencyEntries);
        evidence.Outcome.ShouldBe(DocumentRetentionPurgeOutcome.Refused);
        evidence.PurgedCount.ShouldBe(0);
        evidence.HeldCount.ShouldBe(0);
        var remaining = await Scenario.GetIdempotencyKeys(ct);
        remaining.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Held_candidates_beyond_the_batch_size_do_not_starve_purgeable_ones()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var created = TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays - 10);
        var heldLineage = DocumentDraftInfrastructureTestData.CreateLineageWithRevisions(created, 501);
        var purgeable = DocumentDraftInfrastructureTestData.CreateLineageWithRevisions(created.AddDays(5), 2);
        await Scenario.Seed(heldLineage, purgeable);
        _ = await Harness.Place(heldLineage.Revisions[0].Id, ct);

        // Act
        var counts = await Harness.PurgeDrafts(ct);

        // Assert
        counts.ShouldBe(new DocumentPurgeCounts(2, 501));
        var remaining = await Scenario.GetDocuments(ct);
        remaining.Length.ShouldBe(501);
    }

    [Fact]
    public async Task A_hold_placed_after_a_revision_was_purged_still_restrains_that_revisions_audit_records()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var created = TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays).AddSeconds(-30);
        var lineage = DocumentDraftInfrastructureTestData.CreateLineageWithRevisions(created, 2);
        var purgedRevision = lineage.Revisions[0].Id;
        var keptRevision = lineage.Revisions[1].Id;
        await Scenario.Seed(lineage);
        var audit = DocumentAuditInfrastructureTestData.CreateRecord(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), purgedRevision);
        await Harness.SaveAudit(audit, ct);
        var draftPurge = await Harness.PurgeDrafts(ct);
        _ = await Harness.Place(keptRevision, ct);

        // Act
        var auditPurge = await Harness.PurgeAudits(ct);

        // Assert
        draftPurge.Purged.ShouldBe(1);
        auditPurge.ShouldBe(new DocumentPurgeCounts(0, 1));
        var stillStored = await Scenario.GetAuditRecord(audit.Id, ct);
        stillStored.ShouldNotBeNull();
        var allEvidence = await Scenario.GetPurgeEvidence(ct);
        var evidence = allEvidence.ShouldHaveSingleItem();
        evidence.HeldCount.ShouldBe(1);
    }

    [Fact]
    public async Task An_unheld_audit_record_is_purged_even_after_its_revision_was_purged()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var created = TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays).AddSeconds(-30);
        var lineage = DocumentDraftInfrastructureTestData.CreateLineageWithRevisions(created, 2);
        await Scenario.Seed(lineage);
        var audit = DocumentAuditInfrastructureTestData.CreateRecord(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), lineage.Revisions[0].Id);
        await Harness.SaveAudit(audit, ct);
        _ = await Harness.PurgeDrafts(ct);

        // Act
        var counts = await Harness.PurgeAudits(ct);

        // Assert
        counts.ShouldBe(new DocumentPurgeCounts(1, 0));
        var stored = await Scenario.GetAuditRecord(audit.Id, ct);
        stored.ShouldBeNull();
    }

    [Fact]
    public async Task Held_idempotency_entries_beyond_the_batch_size_do_not_starve_purgeable_ones()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var cutoff = TimeProvider.System.GetUtcNow();
        var lineage = DocumentDraftInfrastructureTestData.CreateLineage(cutoff.UtcDateTime);
        var documentId = lineage.Revisions.ShouldHaveSingleItem().Id;
        await Scenario.Seed(lineage);
        _ = await Harness.Place(documentId, ct);
        var heldScope = $"admin.documents.regenerate-draft:{documentId:N}";
        for (var index = 0; index < 501; index++)
        {
            await Scenario.SeedIdempotencyEntry(heldScope, $"held-{index:D3}", cutoff.AddDays(-40), cutoff.AddDays(-39), ct);
        }

        await Scenario.SeedIdempotencyEntry("admin.documents.generate-contract-draft:booking", "free-1", cutoff.AddDays(-5), cutoff.AddDays(-4), ct);
        await Scenario.SeedIdempotencyEntry("admin.documents.generate-contract-draft:booking", "free-2", cutoff.AddDays(-5), cutoff.AddDays(-4), ct);

        // Act
        var counts = await Harness.PurgeIdempotency(cutoff, ct);

        // Assert
        counts.ShouldBe(new DocumentPurgeCounts(2, 501));
        var remaining = await Scenario.GetIdempotencyKeys(ct);
        remaining.Length.ShouldBe(501);
    }

    [Fact]
    public async Task The_hold_guard_refuses_to_answer_outside_a_transaction()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await Scenario.Seed(DocumentDraftInfrastructureTestData.CreateLineage(TimeProvider.System.GetUtcNow().UtcDateTime));

        // Act
        Func<Task> check = () => Harness.FindHeldLineagesWithoutTransaction(Guid.CreateVersion7(), ct);

        // Assert
        await check.ShouldThrow<DispositionHoldStateUnknownException>();
    }

    [Fact]
    public async Task A_purge_check_waits_for_a_hold_being_placed_and_then_sees_it()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var lineage = DocumentDraftInfrastructureTestData.CreateLineage(TimeProvider.System.GetUtcNow().UtcDateTime);
        await Scenario.Seed(lineage);
        await using var holdingHarness = DocumentHoldPostgreSqlHarness.Create(Scenario.ConnectionString, enableRetryOnFailure: false);
        var commit = await holdingHarness.PlaceWithoutCommitting(lineage.Id, ct);
        var check = Harness.CheckInTransaction(lineage.Id, ct);

        // Act
        await Scenario.WaitForBlockedSession(ct);
        var finishedWhileBlocked = check.IsCompleted;
        await commit();
        var held = await check;

        // Assert
        finishedWhileBlocked.ShouldBeFalse();
        held.ShouldContain(lineage.Id);
    }

    [Fact]
    public async Task Place_release_and_a_rejected_second_release_each_commit_one_audit_record()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var lineage = DocumentDraftInfrastructureTestData.CreateLineage(TimeProvider.System.GetUtcNow().UtcDateTime);
        var documentId = lineage.Revisions.ShouldHaveSingleItem().Id;
        await Scenario.Seed(lineage);

        // Act
        var holdId = await Harness.Place(documentId, ct);
        await Harness.Release(documentId, holdId, ct);
        var second = await Harness.TryRelease(documentId, holdId, ct);
        var unknown = await Harness.TryRelease(Guid.CreateVersion7(), holdId, ct);

        // Assert
        second.Status.ShouldBe(ResultStatus.Conflict);
        unknown.Status.ShouldBe(ResultStatus.NotFound);
        var records = await Scenario.GetDocumentAuditsForDocument(documentId, ct);
        records.Select(record => (record.Operation, record.Outcome, record.ReasonCode)).ShouldBe(
        [
            (DocumentAuditOperation.PlaceHold, DocumentAuditOutcome.Succeeded, DocumentAuditReasonCode.ManualHoldPlacement),
            (DocumentAuditOperation.ReleaseHold, DocumentAuditOutcome.Succeeded, DocumentAuditReasonCode.ManualHoldRelease),
            (DocumentAuditOperation.ReleaseHold, DocumentAuditOutcome.Rejected, DocumentAuditReasonCode.StateConflict),
        ]);
        records.All(record => record.HoldId == holdId).ShouldBeTrue();
        records[0].HoldBasis.ShouldBe(nameof(HoldBasis.LegalClaim));
    }

    [Fact]
    public async Task One_hold_is_read_only_through_its_own_document_lineage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var lineage = DocumentDraftInfrastructureTestData.CreateLineage(TimeProvider.System.GetUtcNow().UtcDateTime);
        var documentId = lineage.Revisions.ShouldHaveSingleItem().Id;
        var other = DocumentDraftInfrastructureTestData.CreateLineage(TimeProvider.System.GetUtcNow().UtcDateTime);
        await Scenario.Seed(lineage);
        await Scenario.Seed(other);
        var holdId = await Harness.Place(documentId, ct);

        // Act
        var found = await Harness.Get(documentId, holdId, ct);
        var wrongDocument = await Harness.Get(other.Revisions.ShouldHaveSingleItem().Id, holdId, ct);
        var unknown = await Harness.Get(documentId, Guid.CreateVersion7(), ct);

        // Assert
        found.Value.Id.ShouldBe(holdId);
        wrongDocument.Status.ShouldBe(ResultStatus.NotFound);
        unknown.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task The_overview_filters_and_pages_holds_across_lineages_in_a_stable_order()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var first = DocumentDraftInfrastructureTestData.CreateLineage(TimeProvider.System.GetUtcNow().UtcDateTime);
        var second = DocumentDraftInfrastructureTestData.CreateLineage(TimeProvider.System.GetUtcNow().UtcDateTime);
        await Scenario.Seed(first);
        await Scenario.Seed(second);
        var firstHold = await Harness.Place(first.Revisions.ShouldHaveSingleItem().Id, ct);
        var secondHold = await Harness.Place(second.Revisions.ShouldHaveSingleItem().Id, ct);
        await Harness.Release(second.Revisions.ShouldHaveSingleItem().Id, secondHold, ct);

        // Act
        var active = await Harness.Overview(new ListDocumentHoldOverviewQuery(DocumentHoldState.Active, null, null, 1, 10), ct);
        var released = await Harness.Overview(new ListDocumentHoldOverviewQuery(DocumentHoldState.Released, HoldBasis.LegalClaim, TimeProvider.System.GetUtcNow().AddMinutes(-5), 1, 10), ct);
        var overdue = await Harness.Overview(new ListDocumentHoldOverviewQuery(DocumentHoldState.OverdueReview, null, null, 1, 10), ct);
        var firstPage = await Harness.Overview(new ListDocumentHoldOverviewQuery(null, null, null, 1, 1), ct);
        var secondPage = await Harness.Overview(new ListDocumentHoldOverviewQuery(null, null, null, 2, 1), ct);

        // Assert
        active.Value.Items.Select(item => item.Hold.Id).ShouldBe([firstHold]);
        released.Value.Items.Select(item => item.Hold.Id).ShouldBe([secondHold]);
        overdue.Value.Items.ShouldBeEmpty();
        firstPage.Value.Items.Select(item => item.Hold.Id).ShouldBe([firstHold]);
        firstPage.Value.HasMore.ShouldBeTrue();
        secondPage.Value.Items.Select(item => item.Hold.Id).ShouldBe([secondHold]);
        secondPage.Value.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task The_overview_resolves_the_newest_revision_per_lineage_and_leaves_a_purged_lineage_unlinked()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var recent = TimeProvider.System.GetUtcNow().UtcDateTime;
        var multi = DocumentDraftInfrastructureTestData.CreateLineageWithRevisions(recent, 3);
        var single = DocumentDraftInfrastructureTestData.CreateLineage(recent);
        var purged = DocumentDraftInfrastructureTestData.CreateLineage(recent.AddDays(-DocumentLimits.DraftRetentionDays - 1));
        await Scenario.Seed(multi);
        await Scenario.Seed(single);
        await Scenario.Seed(purged);
        _ = await Harness.Place(multi.Revisions[0].Id, ct);
        _ = await Harness.Place(single.Revisions.ShouldHaveSingleItem().Id, ct);
        var purgedHold = await Harness.Place(purged.Revisions.ShouldHaveSingleItem().Id, ct);
        await Harness.Release(purged.Revisions.ShouldHaveSingleItem().Id, purgedHold, ct);
        _ = await Harness.PurgeDrafts(ct);

        // Act
        var page = await Harness.Overview(new ListDocumentHoldOverviewQuery(null, null, null, 1, 10), ct);

        // Assert
        var byLineage = page.Value.Items.ToDictionary(item => item.LineageId, item => item.DocumentId);
        byLineage.Count.ShouldBe(3);
        byLineage[multi.Id].ShouldBe(multi.Revisions.OrderByDescending(revision => revision.Revision).First().Id);
        byLineage[single.Id].ShouldBe(single.Revisions.ShouldHaveSingleItem().Id);
        byLineage[purged.Id].ShouldBeNull();
    }
}
