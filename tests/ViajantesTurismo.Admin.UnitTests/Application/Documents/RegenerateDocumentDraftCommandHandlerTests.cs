using SharedKernel.Branding;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing.Builders;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.ApiService;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class RegenerateDocumentDraftCommandHandlerTests
{
    [Fact]
    public async Task Handle_rejects_missing_audit_context_before_adding_a_revision()
    {
        // Arrange
        var document = DocumentDraftTestData.Create(DateTime.UtcNow);
        var tourId = Guid.CreateVersion7();
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
            new FakeBrandingApiClient(DocumentDraftTestData.CreateBrandingSettings()),
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(
            new RegenerateDocumentDraftCommand(document.Id, "booking-confirmation", "2", null!),
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
            new FakeBrandingApiClient(DocumentDraftTestData.CreateBrandingSettings()),
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(
            new RegenerateDocumentDraftCommand(documentId, "booking-confirmation", "2", auditContext),
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
    [InlineData(BookingStatusDto.Confirmed)]
    [InlineData(BookingStatusDto.Completed)]
    public async Task Handle_persists_a_replacement_when_booking_is_accepted(BookingStatusDto bookingStatus)
    {
        // Arrange
        var document = DocumentDraftTestData.Create(DateTime.UtcNow);
        var tourId = Guid.CreateVersion7();
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
            new FakeBrandingApiClient(DocumentDraftTestData.CreateBrandingSettings()),
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(
            new RegenerateDocumentDraftCommand(document.Id, "booking-confirmation", "2", auditContext),
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
    [InlineData(BookingStatusDto.Pending)]
    [InlineData(BookingStatusDto.Cancelled)]
    public async Task Handle_does_not_persist_when_booking_is_not_accepted(BookingStatusDto bookingStatus)
    {
        // Arrange
        var document = DocumentDraftTestData.Create(DateTime.UtcNow);
        var tourId = Guid.CreateVersion7();
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
            new FakeBrandingApiClient(DocumentDraftTestData.CreateBrandingSettings()),
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(
            new RegenerateDocumentDraftCommand(document.Id, "booking-confirmation", "2", auditContext),
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
        var document = DocumentDraftTestData.Create(DateTime.UtcNow);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var auditContext = DocumentAuditTestData.CreateContext();
        var handler = new RegenerateDocumentDraftCommandHandler(
            store,
            new FakeQueryService(null),
            new FakeBrandingApiClient(new BrandingSettingsDto()),
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(
            new RegenerateDocumentDraftCommand(document.Id, "booking-confirmation", "2", auditContext),
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
    public async Task Handle_does_not_save_when_source_tour_is_missing()
    {
        // Arrange
        var document = DocumentDraftTestData.Create(DateTime.UtcNow);
        var booking = DtoBuilders.BuildBookingDto(id: document.BookingId, status: BookingStatusDto.Confirmed);
        var store = new FakeDocumentStore();
        store.Documents.Add(document.Id, document);
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var auditContext = DocumentAuditTestData.CreateContext();
        var handler = new RegenerateDocumentDraftCommandHandler(
            store,
            new FakeQueryService(booking),
            new FakeBrandingApiClient(new BrandingSettingsDto()),
            unitOfWork,
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork));

        // Act
        var result = await handler.Handle(
            new RegenerateDocumentDraftCommand(document.Id, "booking-confirmation", "2", auditContext),
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
