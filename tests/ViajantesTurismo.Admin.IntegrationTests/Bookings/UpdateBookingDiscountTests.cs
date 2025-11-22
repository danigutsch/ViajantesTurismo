using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Helpers;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class UpdateBookingDiscountTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    private const decimal ValidPercentageDiscount = 10m;
    private const decimal ValidAbsoluteDiscount = 150m;
    private const decimal OverAllowedPercentageDiscount = 150m;
    private const decimal AbsoluteDiscountExceedingSubtotal = 3000m;

    [Fact]
    public async Task Can_Update_Booking_Discount_Percentage()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Perc", "Entage", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var updateDto = DtoBuilders.BuildUpdateBookingDiscountDto(DiscountTypeDto.Percentage, ValidPercentageDiscount, "Seasonal offer");

        // Act
        var updateResponse = await Client.UpdateBookingDiscount(booking.Id, updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(DiscountTypeDto.Percentage, updated.DiscountType);
        Assert.Equal(10m, updated.DiscountAmount);
        var expected = PricingHelper.CalculateExpectedBookingPrice(TestDefaults.BaseTourPrice, 0m, TestDefaults.RegularBikePrice, null, ValidPercentageDiscount);
        Assert.Equal(expected, updated.TotalPrice);
    }

    [Fact]
    public async Task Can_Update_Booking_Discount_Absolute()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Abs", "Olute", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var updateDto = DtoBuilders.BuildUpdateBookingDiscountDto(DiscountTypeDto.Absolute, ValidAbsoluteDiscount, "VIP customer");

        // Act
        var updateResponse = await Client.UpdateBookingDiscount(booking.Id, updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(DiscountTypeDto.Absolute, updated.DiscountType);
        Assert.Equal(150m, updated.DiscountAmount);
        var expected = PricingHelper.CalculateExpectedBookingPrice(TestDefaults.BaseTourPrice, 0m, TestDefaults.RegularBikePrice, null, null, ValidAbsoluteDiscount);
        Assert.Equal(expected, updated.TotalPrice);
    }

    [Fact]
    public async Task Update_Booking_Discount_Invalid_Percentage_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Inval", "Percent", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var updateDto = DtoBuilders.BuildUpdateBookingDiscountDto(DiscountTypeDto.Percentage, OverAllowedPercentageDiscount, "Too big");

        // Act
        var response = await Client.UpdateBookingDiscount(booking.Id, updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_Booking_Discount_Absolute_ExceedsSubtotal_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Exceed", "Subtotal", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var updateDto = DtoBuilders.BuildUpdateBookingDiscountDto(DiscountTypeDto.Absolute, AbsoluteDiscountExceedingSubtotal, "Impossible");

        // Act
        var response = await Client.UpdateBookingDiscount(booking.Id, updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_Discount_On_Completed_Booking_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Lock", "Discount", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);
        var confirmResponse = await Client.ConfirmBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var completeResponse = await Client.CompleteBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

        var updateDto = DtoBuilders.BuildUpdateBookingDiscountDto(DiscountTypeDto.Absolute, 50m, "Late attempt");

        // Act
        var response = await Client.UpdateBookingDiscount(booking.Id, updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
