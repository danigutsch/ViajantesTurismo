using PandaBear.Branding;
using PandaBear.Idempotency;
using PandaBear.Results;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Builders;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.ApiService;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class RegenerateDocumentDraftCommandHandlerTests
{
    [Fact]
    public async Task Handle_rejects_an_uninitialized_idempotency_key_before_any_work()
    {
        // Arrange
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RegenerateDocumentDraftCommandHandler(
            store, new FakeQueryService(null), new FakeTourStore(),
            DocumentBrandingTestData.From(new BrandingSettingsDto()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            new DocumentCommandIdempotency(new UnusedIdempotencyStore(), unitOfWork, TimeProvider.System));

        // Act
        var result = await handler.Handle(
            new RegenerateDocumentDraftCommand(Guid.CreateVersion7(), string.Empty, string.Empty, DocumentAuditTestData.CreateContext(), default),
            CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ErrorDetails.ShouldNotBeNull().Detail.ShouldBe("Idempotency-Key is required.");
        auditStore.Records.ShouldBeEmpty();
        store.AddedDocuments.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_returns_conflict_for_an_active_regeneration_key_before_mutable_document_validation()
    {
        // Arrange
        var sourceDocumentId = Guid.CreateVersion7();
        var idempotencyKey = IdempotencyKey.From(Guid.CreateVersion7().ToString("N"));
        var idempotencyScope = IdempotencyScope.From($"admin.documents.regenerate-draft:{sourceDocumentId:N}");
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RegenerateDocumentDraftCommandHandler(
            store,
            new FakeQueryService(null),
            new FakeTourStore(),
            DocumentBrandingTestData.From(new BrandingSettingsDto()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            DocumentIdempotencyTestData.CreateStarted(idempotencyScope, DocumentCommandIdempotency.DigestKey(idempotencyKey), unitOfWork));

        // Act
        var result = await handler.Handle(
            new RegenerateDocumentDraftCommand(
                sourceDocumentId,
                "booking-confirmation",
                "2",
                DocumentAuditTestData.CreateContext(),
                idempotencyKey),
            CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        result.ErrorDetails.ShouldNotBeNull().Detail.ShouldBe(
            "A document revision already exists for this booking. Reload and retry.");
        store.AddedDocuments.ShouldBeEmpty();
        var audit = auditStore.Records.ShouldHaveSingleItem();
        audit.Operation.ShouldBe(DocumentAuditOperation.Regenerate);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
        audit.DocumentId.ShouldBe(sourceDocumentId);
        audit.BookingId.ShouldBeNull();
        audit.DocumentRevision.ShouldBeNull();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_replays_completed_regeneration_before_mutable_document_validation()
    {
        // Arrange
        var sourceDocumentId = Guid.CreateVersion7();
        var replacementDocumentId = Guid.CreateVersion7();
        var idempotencyKey = IdempotencyKey.From(Guid.CreateVersion7().ToString("N"));
        var idempotencyScope = IdempotencyScope.From($"admin.documents.regenerate-draft:{sourceDocumentId:N}");
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RegenerateDocumentDraftCommandHandler(
            store,
            new FakeQueryService(null),
            new FakeTourStore(),
            DocumentBrandingTestData.From(new BrandingSettingsDto()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            DocumentIdempotencyTestData.CreateCompleted(
                idempotencyScope,
                DocumentCommandIdempotency.DigestKey(idempotencyKey),
                replacementDocumentId,
                DocumentCommandIdempotency.CreateRequestFingerprint("booking-confirmation", "2"),
                unitOfWork));

        // Act
        var result = await handler.Handle(
            new RegenerateDocumentDraftCommand(
                sourceDocumentId,
                "booking-confirmation",
                "2",
                DocumentAuditTestData.CreateContext(),
                idempotencyKey),
            CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(replacementDocumentId);
        store.AddedDocuments.ShouldBeEmpty();
        auditStore.Records.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_replays_matching_input_and_rejects_conflicting_input_for_a_completed_key()
    {
        // Arrange
        var tourData = DocumentTourTestData.WithBooking(BookingStatus.Confirmed);
        var document = DocumentDraftTestData.Create(TimeProvider.System.GetUtcNow().UtcDateTime, bookingId: tourData.BookingId);
        var tourId = tourData.TourId;
        var booking = DtoBuilders.BuildBookingDto(
            id: document.BookingId,
            tourId: tourId,
            customerName: "Synthetic Traveler",
            status: BookingStatusDto.Confirmed);
        var tour = DtoBuilders.BuildTourDto(
            id: tourId,
            identifier: "synthetic-regeneration-input",
            name: "Synthetic regeneration input");
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var idempotencyStore = new CapturingNewIdempotencyStore(unitOfWork);
        var handler = new RegenerateDocumentDraftCommandHandler(
            store,
            new FakeQueryService(booking, tour),
            tourData.TourStore,
            DocumentBrandingTestData.From(DocumentDraftTestData.CreateBrandingSettings()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            new DocumentCommandIdempotency(idempotencyStore, unitOfWork, TimeProvider.System));
        var key = IdempotencyKey.From(Guid.CreateVersion7().ToString("N"));
        var auditContext = DocumentAuditTestData.CreateContext();

        // Act
        var first = await handler.Handle(
            new RegenerateDocumentDraftCommand(document.Id, "booking-confirmation", "2", auditContext, key),
            CancellationToken.None);
        var replay = await handler.Handle(
            new RegenerateDocumentDraftCommand(document.Id, "booking-confirmation", "2", auditContext, key),
            CancellationToken.None);
        var conflicting = await handler.Handle(
            new RegenerateDocumentDraftCommand(document.Id, "booking-confirmation", "3", auditContext, key),
            CancellationToken.None);

        // Assert
        first.IsSuccess.ShouldBeTrue();
        replay.IsSuccess.ShouldBeTrue();
        replay.Value.ShouldBe(first.Value);
        conflicting.Status.ShouldBe(ResultStatus.Conflict);
        store.LastLoadedLineage.ShouldNotBeNull().Revisions.Count.ShouldBe(2);
        var audit = auditStore.Records.ShouldHaveSingleItem();
        audit.Operation.ShouldBe(DocumentAuditOperation.Regenerate);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
        audit.DocumentId.ShouldBe(document.Id);
        audit.BookingId.ShouldBeNull();
        audit.DocumentRevision.ShouldBeNull();
        idempotencyStore.StartedRetentionOwner.ShouldBe(store.LastLoadedLineage.ShouldNotBeNull().Id.ToString("N"));
        idempotencyStore.StageCompletionCallCount.ShouldBe(1);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_persists_one_conflict_audit_when_regeneration_idempotency_ownership_is_lost()
    {
        // Arrange
        var tourData = DocumentTourTestData.WithBooking(BookingStatus.Confirmed);
        var document = DocumentDraftTestData.Create(TimeProvider.System.GetUtcNow().UtcDateTime, bookingId: tourData.BookingId);
        var tourId = tourData.TourId;
        var booking = DtoBuilders.BuildBookingDto(
            id: document.BookingId,
            tourId: tourId,
            status: BookingStatusDto.Confirmed);
        var key = IdempotencyKey.From(Guid.CreateVersion7().ToString("N"));
        var scope = IdempotencyScope.From($"admin.documents.regenerate-draft:{document.Id:N}");
        var operation = new IdempotencyOperation(scope, key);
        var competingEntry = new IdempotencyEntry(
            operation,
            IdempotencyEntryState.Started,
            new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero),
            null,
            null);
        var documentStore = new FakeDocumentStore();
        documentStore.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var idempotencyStore = new CapturingNewIdempotencyStore(unitOfWork, competingEntry);
        var handler = new RegenerateDocumentDraftCommandHandler(
            documentStore,
            new FakeQueryService(booking, DtoBuilders.BuildTourDto(id: tourId)),
            tourData.TourStore,
            DocumentBrandingTestData.From(DocumentDraftTestData.CreateBrandingSettings()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            new DocumentCommandIdempotency(idempotencyStore, unitOfWork, TimeProvider.System));

        // Act
        var result = await handler.Handle(
            new RegenerateDocumentDraftCommand(
                document.Id,
                "booking-confirmation",
                "2",
                DocumentAuditTestData.CreateContext(),
                key),
            CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        documentStore.LastLoadedLineage.ShouldNotBeNull().Revisions.ShouldHaveSingleItem();
        var audit = auditStore.Records.ShouldHaveSingleItem();
        audit.Operation.ShouldBe(DocumentAuditOperation.Regenerate);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
        audit.DocumentId.ShouldBe(document.Id);
        audit.BookingId.ShouldBe(document.BookingId);
        audit.DocumentRevision.ShouldBe(document.Revision);
        idempotencyStore.TryStartCallCount.ShouldBe(1);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_rejects_missing_audit_context_before_adding_a_revision()
    {
        // Arrange
        var tourData = DocumentTourTestData.WithBooking(BookingStatus.Confirmed);
        var document = DocumentDraftTestData.Create(TimeProvider.System.GetUtcNow().UtcDateTime, bookingId: tourData.BookingId);
        var tourId = tourData.TourId;
        var booking = DtoBuilders.BuildBookingDto(
            id: document.BookingId,
            tourId: tourId,
            status: BookingStatusDto.Confirmed);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RegenerateDocumentDraftCommandHandler(
            store,
            new FakeQueryService(booking, DtoBuilders.BuildTourDto(id: tourId)),
            tourData.TourStore,
            DocumentBrandingTestData.From(DocumentDraftTestData.CreateBrandingSettings()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(
            new RegenerateDocumentDraftCommand(document.Id, "booking-confirmation", "2", null!, IdempotencyKey.From(Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        store.AddedDocuments.ShouldBeEmpty();
        auditStore.Records.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_does_not_save_when_current_document_is_missing()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        var auditContext = DocumentAuditTestData.CreateContext();
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RegenerateDocumentDraftCommandHandler(
            store,
            new FakeQueryService(null),
            new FakeTourStore(),
            DocumentBrandingTestData.From(DocumentDraftTestData.CreateBrandingSettings()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(
            new RegenerateDocumentDraftCommand(documentId, "booking-confirmation", "2", auditContext, IdempotencyKey.From(Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);
        var audit = auditStore.Records.ShouldHaveSingleItem();

        // Assert
        result.IsFailure.ShouldBeTrue();
        store.AddedDocuments.ShouldBeEmpty();
        audit.Operation.ShouldBe(DocumentAuditOperation.Regenerate);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.DocumentNotFound);
        audit.DocumentId.ShouldBe(documentId);
        audit.BookingId.ShouldBeNull();
        audit.DocumentRevision.ShouldBeNull();
        audit.ActorId.ShouldBe(auditContext.ActorId);
        audit.CorrelationId.ShouldBe(auditContext.CorrelationId);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed, BookingStatusDto.Confirmed)]
    [InlineData(BookingStatus.Completed, BookingStatusDto.Completed)]
    public async Task Handle_persists_a_replacement_when_booking_is_accepted(
        BookingStatus aggregateStatus,
        BookingStatusDto bookingStatus)
    {
        // Arrange
        var tourData = DocumentTourTestData.WithBooking(aggregateStatus);
        var document = DocumentDraftTestData.Create(TimeProvider.System.GetUtcNow().UtcDateTime, bookingId: tourData.BookingId);
        var tourId = tourData.TourId;
        var booking = DtoBuilders.BuildBookingDto(id: document.BookingId, tourId: tourId, status: bookingStatus);
        var tour = DtoBuilders.BuildTourDto(id: tourId);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var auditContext = DocumentAuditTestData.CreateContext();
        var handler = new RegenerateDocumentDraftCommandHandler(
            store,
            new FakeQueryService(booking, tour),
            tourData.TourStore,
            DocumentBrandingTestData.From(DocumentDraftTestData.CreateBrandingSettings()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(
            new RegenerateDocumentDraftCommand(document.Id, "booking-confirmation", "2", auditContext, IdempotencyKey.From(Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var lineage = store.LastLoadedLineage.ShouldNotBeNull();
        var replacement = lineage.Revisions.OrderBy(revision => revision.Revision).Last();
        replacement.ReplacesDocumentId.ShouldBe(document.Id);
        var auditEvent = lineage.GetDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<DocumentLifecycleAuditDomainEvent>();
        auditEvent.Operation.ShouldBe(DocumentAuditOperation.Regenerate);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Cancelled)]
    public async Task Handle_does_not_persist_when_the_tour_aggregate_booking_is_not_accepted(BookingStatus aggregateStatus)
    {
        // Arrange
        var tourData = DocumentTourTestData.WithBooking(aggregateStatus);
        var document = DocumentDraftTestData.Create(TimeProvider.System.GetUtcNow().UtcDateTime, bookingId: tourData.BookingId);
        var tourId = tourData.TourId;
        var booking = DtoBuilders.BuildBookingDto(id: document.BookingId, tourId: tourId, status: BookingStatusDto.Confirmed);
        var tour = DtoBuilders.BuildTourDto(id: tourId);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var auditContext = DocumentAuditTestData.CreateContext();
        var handler = new RegenerateDocumentDraftCommandHandler(
            store,
            new FakeQueryService(booking, tour),
            tourData.TourStore,
            DocumentBrandingTestData.From(DocumentDraftTestData.CreateBrandingSettings()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(
            new RegenerateDocumentDraftCommand(document.Id, "booking-confirmation", "2", auditContext, IdempotencyKey.From(Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);
        var audit = auditStore.Records.ShouldHaveSingleItem();

        // Assert
        result.IsFailure.ShouldBeTrue();
        store.AddedDocuments.ShouldBeEmpty();
        audit.Operation.ShouldBe(DocumentAuditOperation.Regenerate);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
        audit.DocumentId.ShouldBe(document.Id);
        audit.BookingId.ShouldBe(document.BookingId);
        audit.DocumentRevision.ShouldBe(document.Revision);
        audit.ActorId.ShouldBe(auditContext.ActorId);
        audit.CorrelationId.ShouldBe(auditContext.CorrelationId);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_does_not_save_when_source_booking_is_missing()
    {
        // Arrange
        var document = DocumentDraftTestData.Create(TimeProvider.System.GetUtcNow().UtcDateTime);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var auditContext = DocumentAuditTestData.CreateContext();
        var handler = new RegenerateDocumentDraftCommandHandler(
            store,
            new FakeQueryService(null),
            new FakeTourStore(),
            DocumentBrandingTestData.From(new BrandingSettingsDto()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(
            new RegenerateDocumentDraftCommand(document.Id, "booking-confirmation", "2", auditContext, IdempotencyKey.From(Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);
        var audit = auditStore.Records.ShouldHaveSingleItem();

        // Assert
        result.IsFailure.ShouldBeTrue();
        store.AddedDocuments.ShouldBeEmpty();
        audit.Operation.ShouldBe(DocumentAuditOperation.Regenerate);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.BookingNotFound);
        audit.DocumentId.ShouldBe(document.Id);
        audit.BookingId.ShouldBe(document.BookingId);
        audit.DocumentRevision.ShouldBe(document.Revision);
        audit.ActorId.ShouldBe(auditContext.ActorId);
        audit.CorrelationId.ShouldBe(auditContext.CorrelationId);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_does_not_save_when_no_tour_aggregate_owns_the_source_booking()
    {
        // Arrange
        var document = DocumentDraftTestData.Create(TimeProvider.System.GetUtcNow().UtcDateTime);
        var tourId = Guid.CreateVersion7();
        var booking = DtoBuilders.BuildBookingDto(id: document.BookingId, tourId: tourId, status: BookingStatusDto.Confirmed);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RegenerateDocumentDraftCommandHandler(
            store,
            new FakeQueryService(booking, DtoBuilders.BuildTourDto(id: tourId)),
            new FakeTourStore(),
            DocumentBrandingTestData.From(DocumentDraftTestData.CreateBrandingSettings()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(
            new RegenerateDocumentDraftCommand(document.Id, "booking-confirmation", "2", DocumentAuditTestData.CreateContext(), IdempotencyKey.From(Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResultStatus.NotFound);
        store.AddedDocuments.ShouldBeEmpty();
        var audit = auditStore.Records.ShouldHaveSingleItem();
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.BookingNotFound);
        audit.DocumentId.ShouldBe(document.Id);
        audit.BookingId.ShouldBe(document.BookingId);
        audit.DocumentRevision.ShouldBe(document.Revision);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_does_not_save_when_source_tour_is_missing()
    {
        // Arrange
        var tourData = DocumentTourTestData.WithBooking(BookingStatus.Confirmed);
        var document = DocumentDraftTestData.Create(TimeProvider.System.GetUtcNow().UtcDateTime, bookingId: tourData.BookingId);
        var booking = DtoBuilders.BuildBookingDto(id: document.BookingId, status: BookingStatusDto.Confirmed);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var auditContext = DocumentAuditTestData.CreateContext();
        var handler = new RegenerateDocumentDraftCommandHandler(
            store,
            new FakeQueryService(booking),
            tourData.TourStore,
            DocumentBrandingTestData.From(new BrandingSettingsDto()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(
            new RegenerateDocumentDraftCommand(document.Id, "booking-confirmation", "2", auditContext, IdempotencyKey.From(Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);
        var audit = auditStore.Records.ShouldHaveSingleItem();

        // Assert
        result.IsFailure.ShouldBeTrue();
        store.AddedDocuments.ShouldBeEmpty();
        audit.Operation.ShouldBe(DocumentAuditOperation.Regenerate);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.TourNotFound);
        audit.DocumentId.ShouldBe(document.Id);
        audit.BookingId.ShouldBe(document.BookingId);
        audit.DocumentRevision.ShouldBe(document.Revision);
        audit.ActorId.ShouldBe(auditContext.ActorId);
        audit.CorrelationId.ShouldBe(auditContext.CorrelationId);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }
}
