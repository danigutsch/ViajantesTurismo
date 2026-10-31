using PandaBear.DispositionHold;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents.Holds;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentHoldPurgeTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Audit_purge_skips_a_held_lineage_and_records_the_held_count_in_the_evidence()
    {
        // Arrange
        var lineageId = Guid.CreateVersion7();
        var holds = new InMemoryDispositionHoldStore();
        var hold = Hold.Place(DocumentHoldScopes.ForLineage(lineageId), HoldBasis.LegalClaim, "purpose.claim", "authority:case", "actor-1", Now, Now, Now.AddDays(30));
        holds.Holds.Add(hold);
        var guard = new FakeDispositionHoldGuard();
        guard.RestrainingHoldIds.Add(hold.Id);
        var auditStore = new FakeDocumentAuditStore { PurgedCount = 2 };
        auditStore.CandidateLineageIds.Add(lineageId);
        var handler = DocumentAuditTestData.CreatePurgeHandler(auditStore, new FakeUnitOfWork(), Now, guard, holds);

        // Act
        var counts = await handler.Handle(DocumentAuditTestData.CreatePurgeCommand(), TestContext.Current.CancellationToken);

        // Assert
        counts.Held.ShouldBe(1);
        var evidence = auditStore.PurgeRecords.ShouldHaveSingleItem();
        evidence.Outcome.ShouldBe(DocumentAuditOutcome.Succeeded);
        evidence.HeldCount.ShouldBe(1);
        evidence.PurgedCount.ShouldBe(2);
    }

    [Fact]
    public async Task Audit_purge_checks_the_guard_once_with_every_candidate_scope()
    {
        // Arrange
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();
        var guard = new FakeDispositionHoldGuard();
        var auditStore = new FakeDocumentAuditStore();
        auditStore.CandidateLineageIds.AddRange([first, second, first]);
        var handler = DocumentAuditTestData.CreatePurgeHandler(auditStore, new FakeUnitOfWork(), Now, guard);

        // Act
        _ = await handler.Handle(DocumentAuditTestData.CreatePurgeCommand(), TestContext.Current.CancellationToken);

        // Assert
        var scopes = guard.CheckedScopes.ShouldHaveSingleItem();
        scopes.Select(scope => scope.Key).Order().ShouldBe(new[] { first.ToString("N"), second.ToString("N") }.Order());
        scopes.Count(scope => scope.Kind == "document-lineage").ShouldBe(2);
    }

    [Fact]
    public async Task Audit_purge_is_refused_with_evidence_when_hold_state_is_unknown()
    {
        // Arrange
        var unknown = new DispositionHoldStateUnknownException();
        var guard = new FakeDispositionHoldGuard { Failure = unknown };
        var auditStore = new FakeDocumentAuditStore { PurgedCount = 5 };
        auditStore.CandidateLineageIds.Add(Guid.CreateVersion7());
        var handler = DocumentAuditTestData.CreatePurgeHandler(auditStore, new FakeUnitOfWork(), Now, guard);

        // Act
        Func<Task> purge = () => handler.Handle(DocumentAuditTestData.CreatePurgeCommand(), TestContext.Current.CancellationToken);
        var thrown = await purge.ShouldThrow<DispositionHoldStateUnknownException>();

        // Assert
        thrown.ShouldBeSameAs(unknown);
        var evidence = auditStore.PurgeRecords.ShouldHaveSingleItem();
        evidence.Outcome.ShouldBe(DocumentAuditOutcome.Refused);
        evidence.PurgedCount.ShouldBe(0);
        evidence.HeldCount.ShouldBe(0);
    }

    [Fact]
    public async Task Gate_refuses_when_a_restraining_hold_cannot_be_resolved_to_a_lineage()
    {
        // Arrange
        var guard = new FakeDispositionHoldGuard();
        guard.RestrainingHoldIds.Add(Guid.CreateVersion7());
        var gate = new DocumentHoldGate(guard, new InMemoryDispositionHoldStore(), new FakeTimeProvider(Now));

        // Act
        Func<Task> find = () => gate.FindHeldLineages([Guid.CreateVersion7()], TestContext.Current.CancellationToken);

        // Assert
        await find.ShouldThrow<DispositionHoldStateUnknownException>();
    }

    [Fact]
    public async Task Draft_purge_keeps_the_expired_draft_of_a_held_lineage_and_counts_it()
    {
        // Arrange
        var expired = DocumentDraftTestData.Create(Now.UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays));
        var store = new FakeDocumentStore();
        store.Documents.Add(expired.Id, expired);
        var holds = new InMemoryDispositionHoldStore();
        var hold = Hold.Place(DocumentHoldScopes.ForLineage(expired.DocumentLineageId), HoldBasis.Investigation, "purpose.review", "authority:case", "actor-1", Now, Now, Now.AddDays(30));
        holds.Holds.Add(hold);
        var guard = new FakeDispositionHoldGuard();
        guard.RestrainingHoldIds.Add(hold.Id);
        var clock = new FakeTimeProvider(Now);
        var handler = new PurgeExpiredDraftsCommandHandler(store, new FakeDocumentDispositionTransaction(), new DocumentHoldGate(guard, holds, clock), clock);

        // Act
        var counts = await handler.Handle(new PurgeExpiredDraftsCommand(), TestContext.Current.CancellationToken);

        // Assert
        counts.ShouldBe(new DocumentPurgeCounts(0, 1));
        store.Documents.ContainsKey(expired.Id).ShouldBeTrue();
    }

    [Fact]
    public async Task Draft_purge_after_release_removes_the_draft()
    {
        // Arrange
        var expired = DocumentDraftTestData.Create(Now.UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays));
        var store = new FakeDocumentStore();
        store.Documents.Add(expired.Id, expired);
        var holds = new InMemoryDispositionHoldStore();
        var hold = Hold.Place(DocumentHoldScopes.ForLineage(expired.DocumentLineageId), HoldBasis.Investigation, "purpose.review", "authority:case", "actor-1", Now.AddDays(-1), Now.AddDays(-1), Now.AddDays(30));
        hold.Release("actor-2", "reason.closed", Now);
        holds.Holds.Add(hold);
        var clock = new FakeTimeProvider(Now);
        var handler = new PurgeExpiredDraftsCommandHandler(store, new FakeDocumentDispositionTransaction(), new DocumentHoldGate(new FakeDispositionHoldGuard(), holds, clock), clock);

        // Act
        var counts = await handler.Handle(new PurgeExpiredDraftsCommand(), TestContext.Current.CancellationToken);

        // Assert
        counts.ShouldBe(new DocumentPurgeCounts(1, 0));
        store.Documents.ContainsKey(expired.Id).ShouldBeFalse();
    }

    [Fact]
    public async Task Idempotency_purge_aborts_when_hold_state_is_unknown()
    {
        // Arrange
        var store = new FakeDocumentIdempotencyRetentionStore { PurgedCount = 4 };
        store.CandidateLineageIds.Add(Guid.CreateVersion7());
        var guard = new FakeDispositionHoldGuard { Failure = new DispositionHoldStateUnknownException() };
        var clock = new FakeTimeProvider(Now);
        var handler = new PurgeExpiredDocumentIdempotencyEntriesCommandHandler(store, new FakeDocumentDispositionTransaction(), new DocumentHoldGate(guard, new InMemoryDispositionHoldStore(), clock), clock);

        // Act
        Func<Task> purge = () => handler.Handle(TestContext.Current.CancellationToken);

        // Assert
        await purge.ShouldThrow<DispositionHoldStateUnknownException>();
    }
}
