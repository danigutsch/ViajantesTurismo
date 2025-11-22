using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Helpers;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class GetAllBookingsTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Get_Multiple_Bookings()
    {
        // Arrange
        var tourDto = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer1 = await Client.CreateTestCustomer("Alice", "Johnson", cancellationToken: TestContext.Current.CancellationToken);
        var customer2 = await Client.CreateTestCustomer("Charlie", "Brown", cancellationToken: TestContext.Current.CancellationToken);

        var booking1 = await Client.CreateTestBooking(tourDto.Id, customer1.Id, cancellationToken: TestContext.Current.CancellationToken);
        var booking2 = await Client.CreateTestBooking(tourDto.Id, customer2.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetAllBookings(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings = await response.Content.ReadFromJsonAsync<GetBookingDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.True(bookings.Length >= 2);
        Assert.Contains(bookings, b => b.Id == booking1.Id);
        Assert.Contains(bookings, b => b.Id == booking2.Id);
    }
}
