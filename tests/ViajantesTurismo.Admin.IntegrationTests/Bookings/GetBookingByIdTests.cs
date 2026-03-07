using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;
using ViajantesTurismo.Admin.Tests.Shared.Integration.Helpers;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class GetBookingByIdTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Get_Booking_By_Id()
    {
        // Arrange
        var tourDto = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customerDto = await Client.CreateTestCustomer("David", "Wilson", cancellationToken: TestContext.Current.CancellationToken);
        var createdBooking = await Client.CreateTestBooking(tourDto.Id, customerDto.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetBooking(createdBooking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var booking =
            await response.Content.ReadFromJsonAsync<GetBookingDto>(
                TestContext.Current.CancellationToken);
        Assert.NotNull(booking);
        Assert.Equal(createdBooking.Id, booking.Id);
        var expectedPrice = PricingHelper.CalculateExpectedBookingPrice(
            basePrice: 2000m,
            roomSupplement: 0m,
            principalBikePrice: 100m);
        Assert.Equal(expectedPrice, booking.TotalPrice);
    }

    [Fact]
    public async Task Get_Booking_By_Id_Returns_Not_Found_For_Invalid_Id()
    {
        // Arrange
        var nonExistingId = Guid.CreateVersion7();

        // Act
        var response = await Client.GetBooking(nonExistingId,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Can_Get_Booking_With_Payments()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Payment", "Check", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);
        var payment1 = DtoBuilders.BuildCreatePaymentDto(amount: 500m);
        var payment1Response = await Client.RecordPayment(booking.Id, payment1, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, payment1Response.StatusCode);
        var payment2 = DtoBuilders.BuildCreatePaymentDto(amount: 300m);
        var payment2Response = await Client.RecordPayment(booking.Id, payment2, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, payment2Response.StatusCode);

        // Act
        var response = await Client.GetBooking(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookingWithPayments = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(bookingWithPayments);
        Assert.Equal(booking.Id, bookingWithPayments.Id);
        Assert.Equal(800m, bookingWithPayments.AmountPaid);
        Assert.Equal(PaymentStatusDto.PartiallyPaid, bookingWithPayments.PaymentStatus);
        Assert.True(bookingWithPayments.RemainingBalance > 0);
    }
}
