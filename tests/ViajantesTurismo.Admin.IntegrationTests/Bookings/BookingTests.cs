namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SmokeCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.IntegrationScope)]
[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.BookingsArea)]
public class BookingTests(ApiFixture fixture)
{
    [Fact]
    public async Task Can_getbookings_smoke()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await fixture.Client.GetAsync(new Uri("/api/v1/bookings", UriKind.Relative), cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Exposes_the_apphost_managed_baseuri_through_the_host_seam()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var baseUri = fixture.BaseUri;
        var response = await fixture.Client.GetAsync(new Uri("/api/v1/bookings", UriKind.Relative), cancellationToken);

        // Assert
        (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps).ShouldBeTrue();
        string.IsNullOrWhiteSpace(baseUri.Host).ShouldBeFalse();
        (baseUri.Port > 0).ShouldBeTrue();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

}
