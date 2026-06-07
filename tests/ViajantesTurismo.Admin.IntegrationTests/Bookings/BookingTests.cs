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
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await fixture.Client.GetAsync(new Uri("/bookings", UriKind.Relative), cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Exposes_A_Loopback_BaseUri_Through_The_Host_Seam()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var baseUri = fixture.BaseUri;
        var response = await fixture.Client.GetAsync(new Uri("/bookings", UriKind.Relative), cancellationToken);

        // Assert
        Assert.Equal("127.0.0.1", baseUri.Host);
        Assert.True(baseUri.Port > 0);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Exposes_A_Seeded_Baseline_Without_Test_Control_Pruning_The_Host_Seam()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var originalBookings = await fixture.Client.GetAllBookingsAndRead(cancellationToken);

        // Act
        var bookingsAfterSecondRead = await fixture.Client.GetAllBookingsAndRead(cancellationToken);

        Assert.NotEmpty(originalBookings);

        // Assert
        Assert.Equal(originalBookings.Length, bookingsAfterSecondRead.Length);
    }
}
