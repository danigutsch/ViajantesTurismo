using ViajantesTurismo.Admin.Application.Idempotency;
using System.Text.Json;
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
public sealed class GenerateContractDraftCommandHandlerTests
{
    [Fact]
    public async Task Handle_rejects_an_uninitialized_idempotency_key_before_any_work()
    {
        // Arrange
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new GenerateContractDraftCommandHandler(
            new FakeQueryService(null), new FakeTourStore(), store,
            DocumentBrandingTestData.Http(new BrandingSettingsDto()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            new DocumentCommandIdempotency(new UnusedIdempotencyStore(), unitOfWork, TimeProvider.System));

        // Act
        var result = await handler.Handle(
            new GenerateContractDraftCommand(Guid.CreateVersion7(), string.Empty, string.Empty, DocumentAuditTestData.CreateContext(), default(IdempotencyKey)),
            CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ErrorDetails.ShouldNotBeNull().Detail.ShouldBe("Idempotency-Key is required.");
        auditStore.Records.ShouldBeEmpty();
        store.AddedDocuments.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_returns_conflict_for_an_active_generation_key_before_mutable_booking_validation()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        var idempotencyKey = IdempotencyKey.From(Guid.CreateVersion7().ToString("N"));
        var idempotencyScope = IdempotencyScope.From($"admin.documents.generate-contract-draft:{bookingId:N}");
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new GenerateContractDraftCommandHandler(
            new FakeQueryService(null),
            new FakeTourStore(),
            store,
            DocumentBrandingTestData.Http(new BrandingSettingsDto()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            DocumentIdempotencyTestData.CreateStarted(idempotencyScope, idempotencyKey, unitOfWork));

        // Act
        var result = await handler.Handle(
            new GenerateContractDraftCommand(
                bookingId,
                "booking-confirmation",
                "1",
                DocumentAuditTestData.CreateContext(),
                idempotencyKey),
            CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        result.ErrorDetails.ShouldNotBeNull().Detail.ShouldBe(
            "A document revision already exists for this booking. Reload and retry.");
        store.AddedDocuments.ShouldBeEmpty();
        var audit = auditStore.Records.ShouldHaveSingleItem();
        audit.Operation.ShouldBe(DocumentAuditOperation.Generate);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
        audit.DocumentId.ShouldBeNull();
        audit.BookingId.ShouldBe(bookingId);
        audit.DocumentRevision.ShouldBeNull();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_replays_completed_generation_before_mutable_booking_validation()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        var documentId = Guid.CreateVersion7();
        var idempotencyKey = IdempotencyKey.From(Guid.CreateVersion7().ToString("N"));
        var idempotencyScope = IdempotencyScope.From($"admin.documents.generate-contract-draft:{bookingId:N}");
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new GenerateContractDraftCommandHandler(
            new FakeQueryService(null),
            new FakeTourStore(),
            store,
            DocumentBrandingTestData.Http(new BrandingSettingsDto()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            DocumentIdempotencyTestData.CreateCompleted(
                idempotencyScope,
                idempotencyKey,
                documentId,
                ApplicationRequestFingerprint.Create("booking-confirmation", "1"),
                unitOfWork));

        // Act
        var result = await handler.Handle(
            new GenerateContractDraftCommand(
                bookingId,
                "booking-confirmation",
                "1",
                DocumentAuditTestData.CreateContext(),
                idempotencyKey),
            CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(documentId);
        store.AddedDocuments.ShouldBeEmpty();
        auditStore.Records.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_rejects_a_completed_idempotency_key_reused_with_different_template_input()
    {
        // Arrange
        var tourData = DocumentTourTestData.WithBooking(BookingStatus.Confirmed);
        var bookingId = tourData.BookingId;
        var tourId = tourData.TourId;
        var booking = DtoBuilders.BuildBookingDto(
            id: bookingId,
            tourId: tourId,
            customerName: "Synthetic Traveler",
            status: BookingStatusDto.Confirmed);
        var tour = DtoBuilders.BuildTourDto(
            id: tourId,
            identifier: "synthetic-idempotency-input",
            name: "Synthetic idempotency input");
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var idempotencyStore = new CapturingNewIdempotencyStore(unitOfWork);
        var handler = new GenerateContractDraftCommandHandler(
            new FakeQueryService(booking, tour),
            tourData.TourStore,
            store,
            DocumentBrandingTestData.Http(DocumentDraftTestData.CreateBrandingSettings()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            new DocumentCommandIdempotency(idempotencyStore, unitOfWork, TimeProvider.System));
        var key = IdempotencyKey.From(Guid.CreateVersion7().ToString("N"));
        var auditContext = DocumentAuditTestData.CreateContext();

        // Act
        var first = await handler.Handle(
            new GenerateContractDraftCommand(bookingId, "booking-confirmation", "1", auditContext, key),
            CancellationToken.None);
        var conflicting = await handler.Handle(
            new GenerateContractDraftCommand(bookingId, "booking-confirmation", "2", auditContext, key),
            CancellationToken.None);

        // Assert
        first.IsSuccess.ShouldBeTrue();
        conflicting.Status.ShouldBe(ResultStatus.Conflict);
        store.AddedLineages.Count.ShouldBe(1);
        var audit = auditStore.Records.ShouldHaveSingleItem();
        audit.Operation.ShouldBe(DocumentAuditOperation.Generate);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
        audit.DocumentId.ShouldBeNull();
        audit.BookingId.ShouldBe(bookingId);
        audit.DocumentRevision.ShouldBeNull();
        idempotencyStore.StageCompletionCallCount.ShouldBe(1);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_persists_one_metadata_only_conflict_audit_when_idempotency_ownership_is_lost()
    {
        // Arrange
        const string contentMarker = "SYNTHETIC_DOCUMENT_CONTENT_512A";
        const string requestMarker = "SYNTHETIC_TEMPLATE_INPUT_734B";
        var tourData = DocumentTourTestData.WithBooking(BookingStatus.Confirmed);
        var bookingId = tourData.BookingId;
        var tourId = tourData.TourId;
        var key = IdempotencyKey.From(Guid.CreateVersion7().ToString("N"));
        var scope = IdempotencyScope.From($"admin.documents.generate-contract-draft:{bookingId:N}");
        var operation = new IdempotencyOperation(scope, key);
        var competingEntry = new IdempotencyEntry(
            operation,
            IdempotencyEntryState.Started,
            new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero),
            null,
            null);
        var booking = DtoBuilders.BuildBookingDto(
            id: bookingId,
            tourId: tourId,
            customerName: contentMarker,
            status: BookingStatusDto.Confirmed);
        var tour = DtoBuilders.BuildTourDto(id: tourId, name: contentMarker);
        var documentStore = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var idempotencyStore = new CapturingNewIdempotencyStore(unitOfWork, competingEntry);
        var handler = new GenerateContractDraftCommandHandler(
            new FakeQueryService(booking, tour),
            tourData.TourStore,
            documentStore,
            DocumentBrandingTestData.Http(DocumentDraftTestData.CreateBrandingSettings()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            new DocumentCommandIdempotency(idempotencyStore, unitOfWork, TimeProvider.System));

        // Act
        var result = await handler.Handle(
            new GenerateContractDraftCommand(bookingId, requestMarker, "1", DocumentAuditTestData.CreateContext(), key),
            CancellationToken.None);
        var auditJson = JsonSerializer.Serialize(auditStore.Records);

        // Assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        documentStore.AddedDocuments.ShouldBeEmpty();
        var audit = auditStore.Records.ShouldHaveSingleItem();
        audit.Operation.ShouldBe(DocumentAuditOperation.Generate);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
        audit.DocumentId.ShouldBeNull();
        audit.BookingId.ShouldBe(bookingId);
        audit.DocumentRevision.ShouldBeNull();
        auditJson.ShouldNotContain(contentMarker, StringComparison.Ordinal);
        auditJson.ShouldNotContain(requestMarker, StringComparison.Ordinal);
        idempotencyStore.TryStartCallCount.ShouldBe(1);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_rejects_missing_audit_context_before_adding_a_draft()
    {
        // Arrange
        var tourData = DocumentTourTestData.WithBooking(BookingStatus.Confirmed);
        var tourId = tourData.TourId;
        var booking = DtoBuilders.BuildBookingDto(id: tourData.BookingId, tourId: tourId, status: BookingStatusDto.Confirmed);
        var tour = DtoBuilders.BuildTourDto(id: tourId);
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new GenerateContractDraftCommandHandler(
            new FakeQueryService(booking, tour),
            tourData.TourStore,
            store,
            DocumentBrandingTestData.Http(DocumentDraftTestData.CreateBrandingSettings()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(
            new GenerateContractDraftCommand(booking.Id, "booking-confirmation", "1", null!, IdempotencyKey.From(Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        store.AddedDocuments.ShouldBeEmpty();
        auditStore.Records.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed, BookingStatusDto.Confirmed)]
    [InlineData(BookingStatus.Completed, BookingStatusDto.Completed)]
    public async Task Handle_persists_a_classified_draft_when_booking_is_accepted(
        BookingStatus aggregateStatus,
        BookingStatusDto bookingStatus)
    {
        // Arrange
        var tourData = DocumentTourTestData.WithBooking(aggregateStatus);
        var bookingId = tourData.BookingId;
        var tourId = tourData.TourId;
        var queryService = new FakeQueryService(
            new GetBookingDto { Id = bookingId, TourId = tourId, TourIdentifier = "andes", TourName = "Andes", CustomerId = Guid.CreateVersion7(), CustomerName = "Ada", RoomType = RoomTypeDto.DoubleOccupancy, PrincipalBikeType = BikeTypeDto.Regular, BookingDate = TimeProvider.System.GetUtcNow().UtcDateTime, Status = bookingStatus, PaymentStatus = default, TotalPrice = 1200m, DiscountType = default, DiscountAmount = 0m, Currency = default, AmountPaid = 0m, RemainingBalance = 1200m },
            new GetTourDto { Id = tourId, Identifier = "andes", Name = "Andes", StartDate = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime), EndDate = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(7)), Price = 1200m, SingleRoomSupplementPrice = 0m, RegularBikePrice = 0m, EBikePrice = 0m, Currency = default, IncludedServices = ["Guide"], MinCustomers = 1, MaxCustomers = 10, CurrentCustomerCount = 1 });
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var branding = new BrandingSettingsDto { BrandName = "Viajantes", PrimaryColor = "#102030", AccentColor = "#405060", BackgroundColor = "#fdfdfd", TextColor = "#111111", HeadingFontFamily = "Montserrat", BodyFontFamily = "Inter" };
        var handler = new GenerateContractDraftCommandHandler(queryService, tourData.TourStore, store, DocumentBrandingTestData.Http(branding), TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork), DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(new GenerateContractDraftCommand(bookingId, "booking-confirmation", "1", DocumentAuditTestData.CreateContext(), IdempotencyKey.From(Guid.CreateVersion7().ToString("N"))), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var draft = store.AddedDocuments.ShouldHaveSingleItem();
        draft.Fields.ShouldContain(field => field.FieldId == "customer-name");
        draft.BrandingPrimaryColor.ShouldBe("#102030");
        draft.BrandingAccentColor.ShouldBe("#405060");
        draft.BrandingBackgroundColor.ShouldBe("#fdfdfd");
        draft.BrandingTextColor.ShouldBe("#111111");
        draft.BrandingHeadingFontFamily.ShouldBe("Montserrat");
        draft.BrandingBodyFontFamily.ShouldBe("Inter");
        draft.BrandingFooterText.ShouldBe("Viajantes");
        var lineage = store.AddedLineages.ShouldHaveSingleItem();
        var auditEvent = lineage.GetDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<DocumentLifecycleAuditDomainEvent>();
        auditEvent.Operation.ShouldBe(DocumentAuditOperation.Generate);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Cancelled)]
    public async Task Handle_does_not_persist_when_the_tour_aggregate_booking_is_not_accepted(BookingStatus aggregateStatus)
    {
        // Arrange
        var tourData = DocumentTourTestData.WithBooking(aggregateStatus);
        var tourId = tourData.TourId;
        var booking = DtoBuilders.BuildBookingDto(id: tourData.BookingId, tourId: tourId, status: BookingStatusDto.Confirmed);
        var tour = DtoBuilders.BuildTourDto(id: tourId);
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var auditContext = DocumentAuditTestData.CreateContext();
        var handler = new GenerateContractDraftCommandHandler(
            new FakeQueryService(booking, tour),
            tourData.TourStore,
            store,
            DocumentBrandingTestData.Http(DocumentDraftTestData.CreateBrandingSettings()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(
            new GenerateContractDraftCommand(booking.Id, "booking-confirmation", "1", auditContext, IdempotencyKey.From(Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);
        var audit = auditStore.Records.ShouldHaveSingleItem();

        // Assert
        result.IsFailure.ShouldBeTrue();
        store.AddedDocuments.ShouldBeEmpty();
        audit.Operation.ShouldBe(DocumentAuditOperation.Generate);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.StateConflict);
        audit.DocumentId.ShouldBeNull();
        audit.BookingId.ShouldBe(booking.Id);
        audit.DocumentRevision.ShouldBeNull();
        audit.ActorId.ShouldBe(auditContext.ActorId);
        audit.CorrelationId.ShouldBe(auditContext.CorrelationId);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_returns_not_found_without_persisting_when_booking_is_missing()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        var auditContext = DocumentAuditTestData.CreateContext();
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new GenerateContractDraftCommandHandler(new FakeQueryService(null), new FakeTourStore(), store, DocumentBrandingTestData.Http(new BrandingSettingsDto()), TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork), DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(
            new GenerateContractDraftCommand(bookingId, "booking-confirmation", "1", auditContext, IdempotencyKey.From(Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);
        var audit = auditStore.Records.ShouldHaveSingleItem();

        // Assert
        result.IsFailure.ShouldBeTrue();
        store.AddedDocuments.ShouldBeEmpty();
        audit.Operation.ShouldBe(DocumentAuditOperation.Generate);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.BookingNotFound);
        audit.DocumentId.ShouldBeNull();
        audit.BookingId.ShouldBe(bookingId);
        audit.DocumentRevision.ShouldBeNull();
        audit.ActorId.ShouldBe(auditContext.ActorId);
        audit.CorrelationId.ShouldBe(auditContext.CorrelationId);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_returns_not_found_without_persisting_when_no_tour_aggregate_owns_the_booking()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var booking = DtoBuilders.BuildBookingDto(tourId: tourId, status: BookingStatusDto.Confirmed);
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var auditContext = DocumentAuditTestData.CreateContext();
        var handler = new GenerateContractDraftCommandHandler(
            new FakeQueryService(booking, DtoBuilders.BuildTourDto(id: tourId)),
            new FakeTourStore(),
            store,
            DocumentBrandingTestData.Http(DocumentDraftTestData.CreateBrandingSettings()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(
            new GenerateContractDraftCommand(booking.Id, "booking-confirmation", "1", auditContext, IdempotencyKey.From(Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResultStatus.NotFound);
        store.AddedDocuments.ShouldBeEmpty();
        var audit = auditStore.Records.ShouldHaveSingleItem();
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.BookingNotFound);
        audit.BookingId.ShouldBe(booking.Id);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_fails_closed_without_a_partial_draft_when_the_branding_projection_is_unavailable()
    {
        // Arrange
        var tourData = DocumentTourTestData.WithBooking(BookingStatus.Confirmed);
        var booking = DtoBuilders.BuildBookingDto(id: tourData.BookingId, tourId: tourData.TourId, status: BookingStatusDto.Confirmed);
        var tour = DtoBuilders.BuildTourDto(id: tourData.TourId);
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new GenerateContractDraftCommandHandler(
            new FakeQueryService(booking, tour),
            tourData.TourStore,
            store,
            DocumentBrandingTestData.Projection(null),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(
            new GenerateContractDraftCommand(booking.Id, "booking-confirmation", "1", DocumentAuditTestData.CreateContext(), IdempotencyKey.From(Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ResultStatus.Unavailable);
        store.AddedDocuments.ShouldBeEmpty();
        store.AddedLineages.ShouldBeEmpty();
        auditStore.Records.ShouldHaveSingleItem().ReasonCode.ShouldBe(DocumentAuditReasonCode.BrandingUnavailable);
    }

    [Fact]
    public async Task Handle_records_the_projection_source_revision_with_the_branding_snapshot()
    {
        // Arrange
        var tourData = DocumentTourTestData.WithBooking(BookingStatus.Confirmed);
        var booking = DtoBuilders.BuildBookingDto(id: tourData.BookingId, tourId: tourData.TourId, status: BookingStatusDto.Confirmed);
        var tour = DtoBuilders.BuildTourDto(id: tourData.TourId);
        var settings = DocumentDraftTestData.CreateBrandingSettings();
        var store = new FakeDocumentStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new GenerateContractDraftCommandHandler(
            new FakeQueryService(booking, tour),
            tourData.TourStore,
            store,
            DocumentBrandingTestData.Projection(DocumentBrandingTestData.ProjectionMatching(settings, 12)),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(new FakeDocumentAuditStore(), unitOfWork),
            DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(
            new GenerateContractDraftCommand(booking.Id, "booking-confirmation", "1", DocumentAuditTestData.CreateContext(), IdempotencyKey.From(Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var draft = store.AddedDocuments.ShouldHaveSingleItem();
        draft.BrandingSourceRevision.ShouldBe(12);
        draft.BrandingName.ShouldBe(settings.BrandName);
    }

    [Fact]
    public async Task Handle_returns_not_found_without_persisting_when_tour_is_missing()
    {
        // Arrange
        var tourData = DocumentTourTestData.WithBooking(BookingStatus.Confirmed);
        var booking = DtoBuilders.BuildBookingDto(id: tourData.BookingId, status: BookingStatusDto.Confirmed);
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var auditContext = DocumentAuditTestData.CreateContext();
        var handler = new GenerateContractDraftCommandHandler(
            new FakeQueryService(booking),
            tourData.TourStore,
            store,
            DocumentBrandingTestData.Http(DocumentDraftTestData.CreateBrandingSettings()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(
            new GenerateContractDraftCommand(booking.Id, "booking-confirmation", "1", auditContext, IdempotencyKey.From(Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);
        var audit = auditStore.Records.ShouldHaveSingleItem();

        // Assert
        result.IsFailure.ShouldBeTrue();
        store.AddedDocuments.ShouldBeEmpty();
        audit.Operation.ShouldBe(DocumentAuditOperation.Generate);
        audit.Outcome.ShouldBe(DocumentAuditOutcome.Rejected);
        audit.ReasonCode.ShouldBe(DocumentAuditReasonCode.TourNotFound);
        audit.DocumentId.ShouldBeNull();
        audit.BookingId.ShouldBe(booking.Id);
        audit.DocumentRevision.ShouldBeNull();
        audit.ActorId.ShouldBe(auditContext.ActorId);
        audit.CorrelationId.ShouldBe(auditContext.CorrelationId);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_omits_unsafe_branding_logo_uri_from_snapshot()
    {
        // Arrange
        var tourData = DocumentTourTestData.WithBooking(BookingStatus.Confirmed);
        var bookingId = tourData.BookingId;
        var tourId = tourData.TourId;
        var queryService = new FakeQueryService(
            new GetBookingDto { Id = bookingId, TourId = tourId, TourIdentifier = "andes", TourName = "Andes", CustomerId = Guid.CreateVersion7(), CustomerName = "Ada", RoomType = RoomTypeDto.DoubleOccupancy, PrincipalBikeType = BikeTypeDto.Regular, BookingDate = TimeProvider.System.GetUtcNow().UtcDateTime, Status = BookingStatusDto.Confirmed, PaymentStatus = default, TotalPrice = 1200m, DiscountType = default, DiscountAmount = 0m, Currency = default, AmountPaid = 0m, RemainingBalance = 1200m },
            new GetTourDto { Id = tourId, Identifier = "andes", Name = "Andes", StartDate = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime), EndDate = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(7)), Price = 1200m, SingleRoomSupplementPrice = 0m, RegularBikePrice = 0m, EBikePrice = 0m, Currency = default, IncludedServices = ["Guide"], MinCustomers = 1, MaxCustomers = 10, CurrentCustomerCount = 1 });
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var branding = new BrandingSettingsDto { BrandName = "Viajantes", LogoUri = "/\\evil.test/logo.svg", PrimaryColor = "#000", AccentColor = "#000", BackgroundColor = "#fff", TextColor = "#000", HeadingFontFamily = "sans", BodyFontFamily = "sans" };
        var handler = new GenerateContractDraftCommandHandler(queryService, tourData.TourStore, store, DocumentBrandingTestData.Http(branding), TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork), DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(new GenerateContractDraftCommand(bookingId, "booking-confirmation", "1", DocumentAuditTestData.CreateContext(), IdempotencyKey.From(Guid.CreateVersion7().ToString("N"))), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        store.AddedDocuments.ShouldHaveSingleItem().BrandingLogoUri.ShouldBeNull();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }
}
