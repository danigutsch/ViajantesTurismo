using PandaBear.DispositionHold;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents.Holds;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentRetentionPurgeEvidenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Draft_purge_records_succeeded_evidence_with_the_held_count_in_one_save()
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
        var evidence = new FakeDocumentPurgeEvidenceStore();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeTimeProvider(Now);
        var handler = new PurgeExpiredDraftsCommandHandler(store, DocumentPurgeTestData.CreateRunner(evidence, clock, unitOfWork), new DocumentHoldGate(guard, holds, clock), clock);

        // Act
        var counts = await handler.Handle(new PurgeExpiredDraftsCommand(), TestContext.Current.CancellationToken);

        // Assert
        counts.ShouldBe(new DocumentPurgeCounts(0, 1));
        var record = evidence.Records.ShouldHaveSingleItem();
        record.Kind.ShouldBe(DocumentRetentionPurgeKind.Drafts);
        record.Outcome.ShouldBe(DocumentRetentionPurgeOutcome.Succeeded);
        record.PurgedCount.ShouldBe(0);
        record.HeldCount.ShouldBe(1);
        record.OccurredAtUtc.ShouldBe(Now.UtcDateTime);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Draft_purge_records_succeeded_evidence_with_zero_held_when_no_hold_applies()
    {
        // Arrange
        var expired = DocumentDraftTestData.Create(Now.UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays));
        var store = new FakeDocumentStore();
        store.Documents.Add(expired.Id, expired);
        var evidence = new FakeDocumentPurgeEvidenceStore();
        var clock = new FakeTimeProvider(Now);
        var handler = new PurgeExpiredDraftsCommandHandler(store, DocumentPurgeTestData.CreateRunner(evidence, clock), new DocumentHoldGate(new FakeDispositionHoldGuard(), new InMemoryDispositionHoldStore(), clock), clock);

        // Act
        var counts = await handler.Handle(new PurgeExpiredDraftsCommand(), TestContext.Current.CancellationToken);

        // Assert
        counts.ShouldBe(new DocumentPurgeCounts(1, 0));
        var record = evidence.Records.ShouldHaveSingleItem();
        record.Outcome.ShouldBe(DocumentRetentionPurgeOutcome.Succeeded);
        record.PurgedCount.ShouldBe(1);
        record.HeldCount.ShouldBe(0);
    }

    [Fact]
    public async Task Draft_purge_is_refused_with_evidence_and_removes_nothing_when_hold_state_is_unknown()
    {
        // Arrange
        var expired = DocumentDraftTestData.Create(Now.UtcDateTime.AddDays(-DocumentLimits.DraftRetentionDays));
        var store = new FakeDocumentStore();
        store.Documents.Add(expired.Id, expired);
        var unknown = new DispositionHoldStateUnknownException();
        var guard = new FakeDispositionHoldGuard { Failure = unknown };
        var evidence = new FakeDocumentPurgeEvidenceStore();
        var clock = new FakeTimeProvider(Now);
        var handler = new PurgeExpiredDraftsCommandHandler(store, DocumentPurgeTestData.CreateRunner(evidence, clock), new DocumentHoldGate(guard, new InMemoryDispositionHoldStore(), clock), clock);

        // Act
        Func<Task> purge = () => handler.Handle(new PurgeExpiredDraftsCommand(), TestContext.Current.CancellationToken);
        var thrown = await purge.ShouldThrow<DispositionHoldStateUnknownException>();

        // Assert
        thrown.ShouldBeSameAs(unknown);
        store.Documents.ContainsKey(expired.Id).ShouldBeTrue();
        var record = evidence.Records.ShouldHaveSingleItem();
        record.Kind.ShouldBe(DocumentRetentionPurgeKind.Drafts);
        record.Outcome.ShouldBe(DocumentRetentionPurgeOutcome.Refused);
        record.PurgedCount.ShouldBe(0);
        record.HeldCount.ShouldBe(0);
        evidence.DiscardCount.ShouldBe(1);
    }

    [Fact]
    public async Task Idempotency_purge_records_succeeded_evidence_with_the_held_count()
    {
        // Arrange
        var lineageId = Guid.CreateVersion7();
        var holds = new InMemoryDispositionHoldStore();
        var hold = Hold.Place(DocumentHoldScopes.ForLineage(lineageId), HoldBasis.LegalClaim, "purpose.claim", "authority:case", "actor-1", Now, Now, Now.AddDays(30));
        holds.Holds.Add(hold);
        var guard = new FakeDispositionHoldGuard();
        guard.RestrainingHoldIds.Add(hold.Id);
        var retention = new FakeDocumentIdempotencyRetentionStore { PurgedCount = 3 };
        retention.CandidateLineageIds.Add(lineageId);
        var evidence = new FakeDocumentPurgeEvidenceStore();
        var clock = new FakeTimeProvider(Now);
        var handler = new PurgeExpiredDocumentIdempotencyEntriesCommandHandler(retention, DocumentPurgeTestData.CreateRunner(evidence, clock), new DocumentHoldGate(guard, holds, clock), clock);

        // Act
        var counts = await handler.Handle(TestContext.Current.CancellationToken);

        // Assert
        counts.ShouldBe(new DocumentPurgeCounts(3, 1));
        var record = evidence.Records.ShouldHaveSingleItem();
        record.Kind.ShouldBe(DocumentRetentionPurgeKind.IdempotencyEntries);
        record.Outcome.ShouldBe(DocumentRetentionPurgeOutcome.Succeeded);
        record.PurgedCount.ShouldBe(3);
        record.HeldCount.ShouldBe(1);
    }

    [Fact]
    public async Task Idempotency_purge_records_succeeded_evidence_with_zero_held_when_no_hold_applies()
    {
        // Arrange
        var retention = new FakeDocumentIdempotencyRetentionStore { PurgedCount = 2 };
        var evidence = new FakeDocumentPurgeEvidenceStore();
        var clock = new FakeTimeProvider(Now);
        var handler = new PurgeExpiredDocumentIdempotencyEntriesCommandHandler(retention, DocumentPurgeTestData.CreateRunner(evidence, clock), new DocumentHoldGate(new FakeDispositionHoldGuard(), new InMemoryDispositionHoldStore(), clock), clock);

        // Act
        var counts = await handler.Handle(TestContext.Current.CancellationToken);

        // Assert
        counts.ShouldBe(new DocumentPurgeCounts(2, 0));
        var record = evidence.Records.ShouldHaveSingleItem();
        record.PurgedCount.ShouldBe(2);
        record.HeldCount.ShouldBe(0);
    }

    [Fact]
    public async Task Idempotency_purge_is_refused_with_evidence_when_hold_state_is_unknown()
    {
        // Arrange
        var unknown = new DispositionHoldStateUnknownException();
        var guard = new FakeDispositionHoldGuard { Failure = unknown };
        var retention = new FakeDocumentIdempotencyRetentionStore { PurgedCount = 4 };
        retention.CandidateLineageIds.Add(Guid.CreateVersion7());
        var evidence = new FakeDocumentPurgeEvidenceStore();
        var clock = new FakeTimeProvider(Now);
        var handler = new PurgeExpiredDocumentIdempotencyEntriesCommandHandler(retention, DocumentPurgeTestData.CreateRunner(evidence, clock), new DocumentHoldGate(guard, new InMemoryDispositionHoldStore(), clock), clock);

        // Act
        Func<Task> purge = () => handler.Handle(TestContext.Current.CancellationToken);
        var thrown = await purge.ShouldThrow<DispositionHoldStateUnknownException>();

        // Assert
        thrown.ShouldBeSameAs(unknown);
        var record = evidence.Records.ShouldHaveSingleItem();
        record.Kind.ShouldBe(DocumentRetentionPurgeKind.IdempotencyEntries);
        record.Outcome.ShouldBe(DocumentRetentionPurgeOutcome.Refused);
        record.PurgedCount.ShouldBe(0);
        record.HeldCount.ShouldBe(0);
    }

    [Fact]
    public async Task Refused_evidence_that_cannot_be_saved_surfaces_both_failures()
    {
        // Arrange
        var unknown = new DispositionHoldStateUnknownException();
        var guard = new FakeDispositionHoldGuard { Failure = unknown };
        var retention = new FakeDocumentIdempotencyRetentionStore();
        retention.CandidateLineageIds.Add(Guid.CreateVersion7());
        var clock = new FakeTimeProvider(Now);
        var runner = DocumentPurgeTestData.CreateRunner(new FakeDocumentPurgeEvidenceStore(), clock, new ThrowingUnitOfWork(new InvalidOperationException("save failed")));
        var handler = new PurgeExpiredDocumentIdempotencyEntriesCommandHandler(retention, runner, new DocumentHoldGate(guard, new InMemoryDispositionHoldStore(), clock), clock);

        // Act
        Func<Task> purge = () => handler.Handle(TestContext.Current.CancellationToken);
        var thrown = await purge.ShouldThrow<AggregateException>();

        // Assert
        thrown.InnerExceptions.ShouldContain(unknown);
    }
}
