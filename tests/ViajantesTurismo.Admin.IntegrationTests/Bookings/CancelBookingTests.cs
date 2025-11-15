using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Helpers;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class CancelBookingTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Cancel_Booking()
    {
        // Arrange
        var tourDto = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customerDto = await Client.CreateTestCustomer("Laura", "Brown", cancellationToken: TestContext.Current.CancellationToken);
        var createdBooking = await Client.CreateTestBooking(tourDto.Id, customerDto.Id, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatusDto.Pending, createdBooking.Status);

        // Act
        var response = await Client.CancelBooking(createdBooking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cancelledBooking = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(cancelledBooking);
        Assert.Equal(BookingStatusDto.Cancelled, cancelledBooking.Status);
        Assert.Equal(createdBooking.TotalPrice, cancelledBooking.TotalPrice);
        Assert.Equal(createdBooking.PaymentStatus, cancelledBooking.PaymentStatus);
    }

    [Fact]
    public async Task Cancel_Booking_Returns_Not_Found_For_Invalid_Id()
    {
        // Arrange
        var nonExistingId = Guid.CreateVersion7();

        // Act
        var response = await Client.CancelBooking(nonExistingId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_Completed_Booking_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Complete", "Then Cancel", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var confirmResponse = await Client.ConfirmBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var completeResponse = await Client.CompleteBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

        // Act
        var cancelResponse = await Client.CancelBooking(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, cancelResponse.StatusCode);
    }
}
