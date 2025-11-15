using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Helpers;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

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
        var response = await Client.GetAsync(new Uri($"/bookings/{createdBooking.Id}", UriKind.Relative), TestContext.Current.CancellationToken);

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
        var response = await Client.GetAsync(new Uri($"/bookings/{nonExistingId}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
