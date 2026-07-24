using SharedKernel.Branding;
using SharedKernel.Idempotency;
using SharedKernel.Results;
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
public sealed class GenerateContractDraftCommandHandlerTests
{
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
            store,
            new FakeBrandingApiClient(new BrandingSettingsDto()),
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
        auditStore.Records.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
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
            store,
            new FakeBrandingApiClient(new BrandingSettingsDto()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            DocumentIdempotencyTestData.CreateCompleted(idempotencyScope, idempotencyKey, documentId, unitOfWork));

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
    public async Task Handle_rejects_missing_audit_context_before_adding_a_draft()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var booking = DtoBuilders.BuildBookingDto(tourId: tourId, status: BookingStatusDto.Confirmed);
        var tour = DtoBuilders.BuildTourDto(id: tourId);
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new GenerateContractDraftCommandHandler(
            new FakeQueryService(booking, tour),
            store,
            new FakeBrandingApiClient(DocumentDraftTestData.CreateBrandingSettings()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(
            new GenerateContractDraftCommand(booking.Id, "booking-confirmation", "1", null!),
            CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        store.AddedDocuments.ShouldBeEmpty();
        auditStore.Records.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(BookingStatusDto.Confirmed)]
    [InlineData(BookingStatusDto.Completed)]
    public async Task Handle_persists_a_classified_draft_when_booking_is_accepted(BookingStatusDto bookingStatus)
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        var tourId = Guid.CreateVersion7();
        var queryService = new FakeQueryService(
            new GetBookingDto { Id = bookingId, TourId = tourId, TourIdentifier = "andes", TourName = "Andes", CustomerId = Guid.CreateVersion7(), CustomerName = "Ada", RoomType = RoomTypeDto.DoubleOccupancy, PrincipalBikeType = BikeTypeDto.Regular, BookingDate = DateTime.UtcNow, Status = bookingStatus, PaymentStatus = default, TotalPrice = 1200m, DiscountType = default, DiscountAmount = 0m, Currency = default, Payments = [], AmountPaid = 0m, RemainingBalance = 1200m },
            new GetTourDto { Id = tourId, Identifier = "andes", Name = "Andes", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7), Price = 1200m, SingleRoomSupplementPrice = 0m, RegularBikePrice = 0m, EBikePrice = 0m, Currency = default, IncludedServices = ["Guide"], MinCustomers = 1, MaxCustomers = 10, CurrentCustomerCount = 1 });
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var branding = new BrandingSettingsDto { BrandName = "Viajantes", PrimaryColor = "#102030", AccentColor = "#405060", BackgroundColor = "#fdfdfd", TextColor = "#111111", HeadingFontFamily = "Montserrat", BodyFontFamily = "Inter" };
        var handler = new GenerateContractDraftCommandHandler(queryService, store, new FakeBrandingApiClient(branding), TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork), DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(new GenerateContractDraftCommand(bookingId, "booking-confirmation", "1", DocumentAuditTestData.CreateContext()), CancellationToken.None);

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
    [InlineData(BookingStatusDto.Pending)]
    [InlineData(BookingStatusDto.Cancelled)]
    public async Task Handle_does_not_persist_when_booking_is_not_accepted(BookingStatusDto bookingStatus)
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var booking = DtoBuilders.BuildBookingDto(tourId: tourId, status: bookingStatus);
        var tour = DtoBuilders.BuildTourDto(id: tourId);
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var auditContext = DocumentAuditTestData.CreateContext();
        var handler = new GenerateContractDraftCommandHandler(
            new FakeQueryService(booking, tour),
            store,
            new FakeBrandingApiClient(DocumentDraftTestData.CreateBrandingSettings()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(
            new GenerateContractDraftCommand(booking.Id, "booking-confirmation", "1", auditContext),
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
        var handler = new GenerateContractDraftCommandHandler(new FakeQueryService(null), store, new FakeBrandingApiClient(new BrandingSettingsDto()), TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork), DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(
            new GenerateContractDraftCommand(bookingId, "booking-confirmation", "1", auditContext),
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
    public async Task Handle_returns_not_found_without_persisting_when_tour_is_missing()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(status: BookingStatusDto.Confirmed);
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var auditContext = DocumentAuditTestData.CreateContext();
        var handler = new GenerateContractDraftCommandHandler(
            new FakeQueryService(booking),
            store,
            new FakeBrandingApiClient(DocumentDraftTestData.CreateBrandingSettings()),
            TimeProvider.System,
            DocumentAuditTestData.CreateWriter(auditStore, unitOfWork),
            DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(
            new GenerateContractDraftCommand(booking.Id, "booking-confirmation", "1", auditContext),
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
        var bookingId = Guid.CreateVersion7();
        var tourId = Guid.CreateVersion7();
        var queryService = new FakeQueryService(
            new GetBookingDto { Id = bookingId, TourId = tourId, TourIdentifier = "andes", TourName = "Andes", CustomerId = Guid.CreateVersion7(), CustomerName = "Ada", RoomType = RoomTypeDto.DoubleOccupancy, PrincipalBikeType = BikeTypeDto.Regular, BookingDate = DateTime.UtcNow, Status = BookingStatusDto.Confirmed, PaymentStatus = default, TotalPrice = 1200m, DiscountType = default, DiscountAmount = 0m, Currency = default, Payments = [], AmountPaid = 0m, RemainingBalance = 1200m },
            new GetTourDto { Id = tourId, Identifier = "andes", Name = "Andes", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7), Price = 1200m, SingleRoomSupplementPrice = 0m, RegularBikePrice = 0m, EBikePrice = 0m, Currency = default, IncludedServices = ["Guide"], MinCustomers = 1, MaxCustomers = 10, CurrentCustomerCount = 1 });
        var store = new FakeDocumentStore();
        var auditStore = new FakeDocumentAuditStore();
        var unitOfWork = new FakeUnitOfWork();
        var branding = new BrandingSettingsDto { BrandName = "Viajantes", LogoUri = "/\\evil.test/logo.svg", PrimaryColor = "#000", AccentColor = "#000", BackgroundColor = "#fff", TextColor = "#000", HeadingFontFamily = "sans", BodyFontFamily = "sans" };
        var handler = new GenerateContractDraftCommandHandler(queryService, store, new FakeBrandingApiClient(branding), TimeProvider.System, DocumentAuditTestData.CreateWriter(auditStore, unitOfWork), DocumentIdempotencyTestData.Create(unitOfWork));

        // Act
        var result = await handler.Handle(new GenerateContractDraftCommand(bookingId, "booking-confirmation", "1", DocumentAuditTestData.CreateContext()), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        store.AddedDocuments.ShouldHaveSingleItem().BrandingLogoUri.ShouldBeNull();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }
}
