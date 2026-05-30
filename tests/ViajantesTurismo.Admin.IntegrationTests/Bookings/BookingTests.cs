using ViajantesTurismo.Admin.Testing.Integration;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

[Trait(TestTraits.CategoryName, TestTraits.SmokeCategory)]
[Trait(TestTraits.ScopeName, TestTraits.IntegrationScope)]
[Trait(TestTraits.AreaName, TestTraits.BookingsArea)]
public class BookingTests(ApiFixture fixture)
{
    [Fact]
    public async Task Can_GetBookings_Smoke()
    {
        // Arrange
        var host = Assert.IsAssignableFrom<IAdminTestHost>(fixture);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await host.Client.GetAsync(new Uri("/bookings", UriKind.Relative), cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Exposes_A_Loopback_BaseUri_Through_The_Host_Seam()
    {
        // Arrange
        var host = Assert.IsAssignableFrom<IAdminTestHost>(fixture);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var baseUri = host.BaseUri;
        var response = await host.Client.GetAsync(new Uri("/bookings", UriKind.Relative), cancellationToken);

        // Assert
        Assert.Equal("127.0.0.1", baseUri.Host);
        Assert.True(baseUri.Port > 0);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Seed_Is_Idempotent_Through_The_Host_Seam()
    {
        // Arrange
        var host = Assert.IsAssignableFrom<IAdminTestHost>(fixture);
        var cancellationToken = TestContext.Current.CancellationToken;
        var originalBookings = await host.Client.GetAllBookingsAndReadAsync(cancellationToken);

        Assert.NotEmpty(originalBookings);

        // Act
        await host.Seed(cancellationToken);

        var bookingsAfterSeed = await host.Client.GetAllBookingsAndReadAsync(cancellationToken);

        // Assert
        Assert.Equal(originalBookings.Length, bookingsAfterSeed.Length);
    }
}
