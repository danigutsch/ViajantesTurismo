using PandaBear.DispositionHold;
using PandaBear.Testing;
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
        var finishedEarly = await Task.WhenAny(check, Task.Delay(TimeSpan.FromSeconds(1), ct)) == check;
        await commit();
        var held = await check;

        // Assert
        finishedEarly.ShouldBeFalse();
        held.ShouldContain(lineage.Id);
    }
}
