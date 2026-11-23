using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.EndpointCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, PandaBear.Testing.TestTraitValues.TestServerHost)]
public sealed class BookingPageEndpointTests
{
    [Fact]
    public async Task Admin_can_read_a_bounded_booking_page()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (testHost, queries) = AdminHypermediaPathBase.CreateHost();
        await using var factory = testHost;
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        using var response = await client.GetAsync(
            new Uri($"{AdminHypermediaPathBase.Prefix}/api/v1/bookings/page?page=2&sort=customerName", UriKind.Relative), ct);
        var page = await response.Content.ReadFromJsonAsync<GetBookingPageDto>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        page.ShouldNotBeNull();
        page.Counts.ShouldBe(new BookingOverviewCountsDto(1, 1, 0, 0, 0));
        var booking = page.Items.ShouldHaveSingleItem();
        booking.Id.ShouldBe(queries.Booking.Id);
        booking.Links.ShouldNotBeNull();
        booking.Links.ShouldContain(link => link.Href.Contains(AdminHypermediaPathBase.Prefix + "/api/v1/bookings", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Viewer", HttpStatusCode.Forbidden)]
    public async Task Reads_require_booking_and_payment_access(string? role, HttpStatusCode status)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        if (role is not null)
        {
            AdminApiTestHost.ConfigureAuthenticatedClient(client, role);
        }

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/bookings/page", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(status);
    }

    [Fact]
    public async Task Unrecognized_sort_key_is_rejected()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Reader");

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/bookings/page?sort=invalid", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
