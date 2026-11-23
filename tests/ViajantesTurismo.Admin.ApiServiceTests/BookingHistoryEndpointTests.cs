using PandaBear.Testing;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiServiceTests;

/// <summary>Exercises owner-scoped booking page routes and their grant requirements.</summary>
[Trait(TestTraitNames.HostName, TestTraitValues.TestServerHost)]
public sealed class BookingHistoryEndpointTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task An_authorized_owner_page_preserves_the_booking_resource_links(bool byTour)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (testHost, queries) = AdminHypermediaPathBase.CreateHost();
        await using var factory = testHost;
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Reader");
        var expected = queries.Booking;
        var owner = byTour ? $"tour/{expected.TourId}" : $"customer/{expected.CustomerId}";

        // Act
        using var response = await client.GetAsync(new Uri(
            $"{AdminHypermediaPathBase.Prefix}/api/v1/bookings/{owner}/page?page=1&sort=bookingDate&descending=true",
            UriKind.Relative), ct);
        var page = await response.Content.ReadFromJsonAsync<GetBookingHistoryPageDto>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        page.ShouldNotBeNull().Total.ShouldBe(1);
        var booking = page.Items.ShouldHaveSingleItem();
        booking.Id.ShouldBe(expected.Id);
        booking.Links.ShouldNotBeNull().ShouldContain(link =>
            link.Href.Contains(AdminHypermediaPathBase.Prefix + "/api/v1/bookings", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("tour", null, HttpStatusCode.Unauthorized)]
    [InlineData("customer", null, HttpStatusCode.Unauthorized)]
    [InlineData("tour", "Viewer", HttpStatusCode.Forbidden)]
    [InlineData("customer", "Viewer", HttpStatusCode.Forbidden)]
    public async Task Reading_an_owner_history_requires_existing_booking_and_payment_grants(string owner, string? role, HttpStatusCode status)
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
        using var response = await client.GetAsync(new Uri($"/api/v1/bookings/{owner}/{Guid.Empty}/page", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(status);
    }

    [Theory]
    [InlineData("tour", "unknown")]
    [InlineData("tour", "99")]
    [InlineData("customer", "unknown")]
    [InlineData("customer", "99")]
    public async Task An_undefined_sort_key_is_rejected(string owner, string sort)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Reader");

        // Act
        using var response = await client.GetAsync(new Uri($"/api/v1/bookings/{owner}/{Guid.Empty}/page?sort={sort}", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
