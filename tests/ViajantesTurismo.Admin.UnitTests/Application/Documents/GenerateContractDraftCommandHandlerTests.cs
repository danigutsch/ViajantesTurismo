using SharedKernel.Branding;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.ApiService;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, global::ViajantesTurismo.Admin.Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class GenerateContractDraftCommandHandlerTests
{
    [Fact]
    public async Task Handle_persists_a_classified_draft_when_booking_and_tour_exist()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        var tourId = Guid.CreateVersion7();
        var queryService = new FakeQueryService(
            new GetBookingDto { Id = bookingId, TourId = tourId, TourIdentifier = "andes", TourName = "Andes", CustomerId = Guid.CreateVersion7(), CustomerName = "Ada", BookingDate = DateTime.UtcNow, Status = default, PaymentStatus = default, TotalPrice = 1200m, DiscountType = default, DiscountAmount = 0m, Currency = default, Payments = [], AmountPaid = 0m, RemainingBalance = 1200m },
            new GetTourDto { Id = tourId, Identifier = "andes", Name = "Andes", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7), Price = 1200m, SingleRoomSupplementPrice = 0m, RegularBikePrice = 0m, EBikePrice = 0m, Currency = default, IncludedServices = ["Guide"], MinCustomers = 1, MaxCustomers = 10, CurrentCustomerCount = 1 });
        var store = new FakeDocumentStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new GenerateContractDraftCommandHandler(queryService, store, new FakeBrandingApiClient(new BrandingSettingsDto { BrandName = "Viajantes", PrimaryColor = "#000", AccentColor = "#000", BackgroundColor = "#fff", TextColor = "#000", HeadingFontFamily = "sans", BodyFontFamily = "sans" }), unitOfWork, TimeProvider.System);

        // Act
        var result = await handler.Handle(new GenerateContractDraftCommand(bookingId, "booking-confirmation", "1"), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        store.AddedDocuments.ShouldHaveSingleItem().Fields.ShouldContain(field => field.FieldId == "customer-name");
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_returns_not_found_without_persisting_when_booking_is_missing()
    {
        // Arrange
        var store = new FakeDocumentStore();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new GenerateContractDraftCommandHandler(new FakeQueryService(null), store, new FakeBrandingApiClient(new BrandingSettingsDto()), unitOfWork, TimeProvider.System);

        // Act
        var result = await handler.Handle(new GenerateContractDraftCommand(Guid.CreateVersion7(), "booking-confirmation", "1"), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        store.AddedDocuments.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_omits_unsafe_branding_logo_uri_from_snapshot()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        var tourId = Guid.CreateVersion7();
        var queryService = new FakeQueryService(
            new GetBookingDto { Id = bookingId, TourId = tourId, TourIdentifier = "andes", TourName = "Andes", CustomerId = Guid.CreateVersion7(), CustomerName = "Ada", BookingDate = DateTime.UtcNow, Status = default, PaymentStatus = default, TotalPrice = 1200m, DiscountType = default, DiscountAmount = 0m, Currency = default, Payments = [], AmountPaid = 0m, RemainingBalance = 1200m },
            new GetTourDto { Id = tourId, Identifier = "andes", Name = "Andes", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7), Price = 1200m, SingleRoomSupplementPrice = 0m, RegularBikePrice = 0m, EBikePrice = 0m, Currency = default, IncludedServices = ["Guide"], MinCustomers = 1, MaxCustomers = 10, CurrentCustomerCount = 1 });
        var store = new FakeDocumentStore();
        var unitOfWork = new FakeUnitOfWork();
        var branding = new BrandingSettingsDto { BrandName = "Viajantes", LogoUri = "/\\evil.test/logo.svg", PrimaryColor = "#000", AccentColor = "#000", BackgroundColor = "#fff", TextColor = "#000", HeadingFontFamily = "sans", BodyFontFamily = "sans" };
        var handler = new GenerateContractDraftCommandHandler(queryService, store, new FakeBrandingApiClient(branding), unitOfWork, TimeProvider.System);

        // Act
        var result = await handler.Handle(new GenerateContractDraftCommand(bookingId, "booking-confirmation", "1"), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        store.AddedDocuments.ShouldHaveSingleItem().BrandingLogoUri.ShouldBeNull();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }
}
