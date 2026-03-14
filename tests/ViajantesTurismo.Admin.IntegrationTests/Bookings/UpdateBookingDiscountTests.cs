using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;
using ViajantesTurismo.Admin.Tests.Shared.Builders;
using ViajantesTurismo.Admin.Tests.Shared.Integration.Helpers;

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

    [Fact]
    public async Task Update_Booking_Discount_With_Negative_Amount_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Neg", "Discount", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var updateDto = DtoBuilders.BuildUpdateBookingDiscountDto(DiscountTypeDto.Absolute, -100m, "Negative discount");

        // Act
        var response = await Client.UpdateBookingDiscount(booking.Id, updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Can_Remove_Discount_By_Setting_To_None()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Remove", "Discount", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var applyDiscountDto = DtoBuilders.BuildUpdateBookingDiscountDto(DiscountTypeDto.Percentage, 10m, "Early bird");
        await Client.UpdateBookingDiscount(booking.Id, applyDiscountDto, TestContext.Current.CancellationToken);

        var removeDiscountDto = DtoBuilders.BuildUpdateBookingDiscountDto(DiscountTypeDto.None, 0m, "Discount removed");

        // Act
        var response = await Client.UpdateBookingDiscount(booking.Id, removeDiscountDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(DiscountTypeDto.None, updated.DiscountType);
        Assert.Equal(0m, updated.DiscountAmount);
        var expectedPrice = PricingHelper.CalculateExpectedBookingPrice(TestDefaults.BaseTourPrice, 0m, TestDefaults.RegularBikePrice);
        Assert.Equal(expectedPrice, updated.TotalPrice);
    }

    [Fact]
    public async Task Can_Set_Zero_Percentage_Discount_Which_Effectively_Removes_It()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Zero", "Percentage", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var applyDiscountDto = DtoBuilders.BuildUpdateBookingDiscountDto(DiscountTypeDto.Percentage, 15m, "Early bird");
        await Client.UpdateBookingDiscount(booking.Id, applyDiscountDto, TestContext.Current.CancellationToken);

        var updateDto = DtoBuilders.BuildUpdateBookingDiscountDto(DiscountTypeDto.Percentage, 0m, "Zero percent");

        // Act
        var response = await Client.UpdateBookingDiscount(booking.Id, updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(DiscountTypeDto.Percentage, updated.DiscountType);
        Assert.Equal(0m, updated.DiscountAmount);
        var expectedPrice = PricingHelper.CalculateExpectedBookingPrice(TestDefaults.BaseTourPrice, 0m, TestDefaults.RegularBikePrice, null, 0m);
        Assert.Equal(expectedPrice, updated.TotalPrice);
    }

    [Fact]
    public async Task Update_Booking_Discount_Returns_NotFound_For_Invalid_Id()
    {
        // Arrange
        var invalidBookingId = Guid.NewGuid();
        var updateDto = DtoBuilders.BuildUpdateBookingDiscountDto(DiscountTypeDto.Percentage, ValidPercentageDiscount, "Invalid ID");

        // Act
        var response = await Client.UpdateBookingDiscount(invalidBookingId, updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
