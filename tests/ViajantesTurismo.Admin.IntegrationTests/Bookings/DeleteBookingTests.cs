using System.Net;
using ViajantesTurismo.Admin.IntegrationTests.Helpers;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class DeleteBookingTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Delete_Booking()
    {
        // Arrange
        var tourDto = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customerDto = await Client.CreateTestCustomer("Kate", "White", cancellationToken: TestContext.Current.CancellationToken);
        var createdBooking = await Client.CreateTestBooking(tourDto.Id, customerDto.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var response = await Client.DeleteAsync(new Uri($"/bookings/{createdBooking.Id}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await Client.GetAsync(new Uri($"/bookings/{createdBooking.Id}", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_Booking_Returns_Not_Found_For_Invalid_Id()
    {
        // Arrange
        var nonExistingId = Guid.CreateVersion7();

        // Act
        var response = await Client.DeleteAsync(new Uri($"/bookings/{nonExistingId}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
