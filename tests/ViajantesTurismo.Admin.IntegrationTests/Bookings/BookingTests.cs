namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

[Trait(global::SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SmokeCategory)]
[Trait(global::SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.IntegrationScope)]
[Trait(global::SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.BookingsArea)]
public class BookingTests(ApiFixture fixture)
{
    [Fact]
    public async Task Can_getbookings_smoke()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await fixture.Client.GetAsync(new Uri("/bookings", UriKind.Relative), cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Exposes_the_apphost_managed_baseuri_through_the_host_seam()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var baseUri = fixture.BaseUri;
        var response = await fixture.Client.GetAsync(new Uri("/bookings", UriKind.Relative), cancellationToken);

        // Assert
        Assert.Equal(Uri.UriSchemeHttp, baseUri.Scheme);
        Assert.False(string.IsNullOrWhiteSpace(baseUri.Host));
        Assert.True(baseUri.Port > 0);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Exposes_a_seeded_baseline_without_test_control_pruning_the_host_seam()
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
