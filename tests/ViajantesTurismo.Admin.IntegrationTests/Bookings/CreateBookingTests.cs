using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;
using ViajantesTurismo.Admin.Tests.Shared.Builders;
using ViajantesTurismo.Admin.Tests.Shared.Integration.Helpers;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class CreateBookingTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Create_Booking()
    {
        // Arrange
        var tourDto = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customerDto = await Client.CreateTestCustomer("John", "Doe", cancellationToken: TestContext.Current.CancellationToken);

        var bookingRequest = DtoBuilders.BuildCreateBookingDto(
            tourId: tourDto.Id,
            principalCustomerId: customerDto.Id,
            notes: "Test booking");

        // Act
        var response = await Client.CreateBooking(bookingRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<GetBookingDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(booking);
        Assert.Equal(tourDto.Id, booking.TourId);
        Assert.Equal(customerDto.Id, booking.CustomerId);
        var expectedPrice = PricingHelper.CalculateExpectedBookingPrice(
            basePrice: TestDefaults.BaseTourPrice,
            roomSupplement: 0m,
            principalBikePrice: TestDefaults.RegularBikePrice);
        Assert.Equal(expectedPrice, booking.TotalPrice);
        Assert.Equal("Test booking", booking.Notes);
    }

    [Fact]
    public async Task Can_Create_Booking_With_Companion()
    {
        // Arrange
        var tourDto = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customerDto = await Client.CreateTestCustomer("Jane", "Smith", cancellationToken: TestContext.Current.CancellationToken);
        var companionDto = await Client.CreateTestCustomer("Bob", "Smith", cancellationToken: TestContext.Current.CancellationToken);

        var bookingRequest = DtoBuilders.BuildCreateBookingDto(
            tourId: tourDto.Id,
            principalCustomerId: customerDto.Id,
            companionCustomerId: companionDto.Id,
            companionBikeType: BikeTypeDto.Regular,
            roomType: RoomTypeDto.DoubleOccupancy,
            notes: "Couple booking");

        // Act
        var response = await Client.CreateBooking(bookingRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(booking);
        Assert.Equal(companionDto.Id, booking.CompanionId);
        Assert.Contains("Bob Smith", booking.CompanionName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_Booking_Returns_Not_Found_For_Invalid_Tour_Id()
    {
        // Arrange
        var nonExistingTourId = Guid.CreateVersion7();
        var customerDto = await Client.CreateTestCustomer("Test", "User", cancellationToken: TestContext.Current.CancellationToken);

        var bookingRequest = DtoBuilders.BuildCreateBookingDto(
            tourId: nonExistingTourId,
            principalCustomerId: customerDto.Id);

        // Act
        var response = await Client.CreateBooking(bookingRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Booking_Returns_Not_Found_For_Invalid_Customer_Id()
    {
        // Arrange
        var nonExistingCustomerId = Guid.CreateVersion7();
        var tourDto = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);

        var bookingRequest = DtoBuilders.BuildCreateBookingDto(
            tourId: tourDto.Id,
            principalCustomerId: nonExistingCustomerId);

        // Act
        var response = await Client.CreateBooking(bookingRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Can_Create_Booking_With_Percentage_Discount()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Disc", "Percent", cancellationToken: TestContext.Current.CancellationToken);

        var bookingRequest = DtoBuilders.BuildCreateBookingDto(
            tourId: tour.Id,
            principalCustomerId: customer.Id,
            discountType: DiscountTypeDto.Percentage,
            discountAmount: 15m,
            discountReason: "Early bird");

        // Act
        var response = await Client.CreateBooking(bookingRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(booking);
        Assert.Equal(DiscountTypeDto.Percentage, booking.DiscountType);
        Assert.Equal(15m, booking.DiscountAmount);
        var expectedPrice = PricingHelper.CalculateExpectedBookingPrice(
            TestDefaults.BaseTourPrice, 0m, TestDefaults.RegularBikePrice, discountPercentage: 15m);
        Assert.Equal(expectedPrice, booking.TotalPrice);
    }

    [Fact]
    public async Task Can_Create_Booking_With_Absolute_Discount()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Disc", "Absolute", cancellationToken: TestContext.Current.CancellationToken);

        var bookingRequest = DtoBuilders.BuildCreateBookingDto(
            tourId: tour.Id,
            principalCustomerId: customer.Id,
            discountType: DiscountTypeDto.Absolute,
            discountAmount: 200m,
            discountReason: "VIP discount");

        // Act
        var response = await Client.CreateBooking(bookingRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(booking);
        Assert.Equal(DiscountTypeDto.Absolute, booking.DiscountType);
        Assert.Equal(200m, booking.DiscountAmount);
        var expectedPrice = PricingHelper.CalculateExpectedBookingPrice(
            TestDefaults.BaseTourPrice, 0m, TestDefaults.RegularBikePrice, absoluteDiscount: 200m);
        Assert.Equal(expectedPrice, booking.TotalPrice);
    }
}
