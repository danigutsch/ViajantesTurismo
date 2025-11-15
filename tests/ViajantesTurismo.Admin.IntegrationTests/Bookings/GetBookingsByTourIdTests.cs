using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Helpers;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class GetBookingsByTourIdTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Get_Bookings_By_Tour_Id()
    {
        // Arrange
        var tourDto = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer1 = await Client.CreateTestCustomer("Emma", "Davis", cancellationToken: TestContext.Current.CancellationToken);
        var customer2 = await Client.CreateTestCustomer("Frank", "Miller", cancellationToken: TestContext.Current.CancellationToken);

        await Client.CreateTestBooking(tourDto.Id, customer1.Id, cancellationToken: TestContext.Current.CancellationToken);
        await Client.CreateTestBooking(tourDto.Id, customer2.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetAsync(new Uri($"/bookings/tour/{tourDto.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings =
            await response.Content.ReadFromJsonAsync<GetBookingDto[]>(
                TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Equal(2, bookings.Length);
        Assert.All(bookings, b => Assert.Equal(tourDto.Id, b.TourId));
    }

    [Fact]
    public async Task Get_Bookings_By_Tour_Id_Returns_Empty_For_Invalid_Tour()
    {
        // Act
        var response = await Client.GetAsync(new Uri($"/bookings/tour/{Guid.CreateVersion7()}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings = await response.Content.ReadFromJsonAsync<GetBookingDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Empty(bookings);
    }
}
