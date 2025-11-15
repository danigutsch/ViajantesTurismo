using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Helpers;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class CompleteBookingTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Complete_Booking_After_Confirm()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Finish", "Me", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);
        var confirmResponse = await Client.ConfirmBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        // Act
        var completeResponse = await Client.CompleteBooking(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var completed = await completeResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(completed);
        Assert.Equal(BookingStatusDto.Completed, completed.Status);
    }

    [Fact]
    public async Task Complete_Booking_Returns_NotFound_For_Invalid_Id()
    {
        // Arrange
        var nonExistingId = Guid.CreateVersion7();

        // Act
        var response = await Client.CompleteBooking(nonExistingId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Complete_Pending_Booking_Without_Confirm_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Skip", "Confirm", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var completeResponse = await Client.CompleteBooking(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, completeResponse.StatusCode);
    }
}
