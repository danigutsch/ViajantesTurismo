using System.Text.Json;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing.Builders;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraitValues.TestServerHost)]
public sealed class AdminHypermediaEndpointTests
{
    [Theory]
    [InlineData("customers")]
    [InlineData("bookings")]
    [InlineData("tours")]
    public async Task Item_and_list_links_follow_the_API_mount_path(string collection)
    {
        // Arrange
        var (factory, queries) = AdminHypermediaPathBase.CreateHost();
        await using var host = factory;
        using var client = host.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var ct = TestContext.Current.CancellationToken;
        var id = collection switch
        {
            "customers" => queries.Customer.Id,
            "bookings" => queries.Booking.Id,
            "tours" => queries.Tour.Id,
            _ => throw new ArgumentOutOfRangeException(nameof(collection))
        };
        var listPath = $"{AdminHypermediaPathBase.Prefix}/api/v1/{collection}";
        var itemPath = $"{listPath}/{id}";

        // Act
        var list = await client.GetFromJsonAsync<JsonElement>(new Uri($"{listPath}?ignored=1", UriKind.Relative), ct);
        var detail = await client.GetFromJsonAsync<JsonElement>(new Uri(itemPath, UriKind.Relative), ct);
        using var missing = await client.GetAsync(new Uri($"{listPath}/{Guid.CreateVersion7()}", UriKind.Relative), ct);

        // Assert
        list.ValueKind.ShouldBe(JsonValueKind.Array);
        var item = list.EnumerateArray().ShouldHaveSingleItem();
        item.GetProperty("id").GetGuid().ShouldBe(id);
        item.GetProperty("links").EnumerateArray().ShouldHaveSingleItem(link => link.GetProperty("rel").GetString() == "self")
            .GetProperty("href").GetString().ShouldBe(itemPath);
        detail.GetProperty("links").EnumerateArray().ShouldHaveSingleItem(link => link.GetProperty("rel").GetString() == "collection")
            .GetProperty("href").GetString().ShouldBe(listPath);
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("tour")]
    [InlineData("customer")]
    public async Task Filtered_booking_links_point_to_the_same_canonical_resource(string filter)
    {
        // Arrange
        var (factory, queries) = AdminHypermediaPathBase.CreateHost();
        await using var host = factory;
        using var client = host.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var filterId = filter == "tour" ? queries.Booking.TourId : queries.Booking.CustomerId;
        var collection = $"{AdminHypermediaPathBase.Prefix}/api/v1/bookings";

        // Act
        var result = await client.GetFromJsonAsync<GetBookingDto[]>(
            new Uri($"{collection}/{filter}/{filterId}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        var booking = result.ShouldNotBeNull().ShouldHaveSingleItem();
        booking.Id.ShouldBe(queries.Booking.Id);
        booking.Links.ShouldNotBeNull().ShouldHaveSingleItem(link => link.Rel == "self")
            .Href.ShouldBe($"{collection}/{queries.Booking.Id}");
    }

    [Fact]
    public async Task Create_locations_and_links_include_the_API_mount_path()
    {
        // Arrange
        var (factory, _) = AdminHypermediaPathBase.CreateHost();
        await using var host = factory;
        using var client = host.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var origin = $"{AdminHypermediaPathBase.Prefix}/api/v1";
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var customerResponse = await client.PostAsJsonAsync(new Uri($"{origin}/customers", UriKind.Relative),
            DtoBuilders.BuildCreateCustomerDto(), ct);
        var customerPayload = await customerResponse.Content.ReadFromJsonAsync<GetCustomerDto>(ct);
        var customer = customerPayload.ShouldNotBeNull();
        using var tourResponse = await client.PostAsJsonAsync(new Uri($"{origin}/tours", UriKind.Relative),
            DtoBuilders.BuildCreateTourDto(), ct);
        var tourPayload = await tourResponse.Content.ReadFromJsonAsync<GetTourDto>(ct);
        var tour = tourPayload.ShouldNotBeNull();
        using var bookingResponse = await client.PostAsJsonAsync(new Uri($"{origin}/bookings", UriKind.Relative),
            DtoBuilders.BuildCreateBookingDto(tourId: tour.Id, principalCustomerId: customer.Id), ct);
        var booking = await bookingResponse.Content.ReadFromJsonAsync<GetBookingDto>(ct);

        // Assert
        customerResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        customerResponse.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe($"{origin}/customers/{customer.Id}");
        customer.Links.ShouldNotBeNull().ShouldHaveSingleItem(link => link.Rel == "self")
            .Href.ShouldBe(customerResponse.Headers.Location.OriginalString);
        tourResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        tourResponse.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe($"{origin}/tours/{tour.Id}");
        tour.Links.ShouldNotBeNull().ShouldHaveSingleItem(link => link.Rel == "self")
            .Href.ShouldBe(tourResponse.Headers.Location.OriginalString);
        bookingResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        booking.ShouldNotBeNull();
        bookingResponse.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe($"{origin}/bookings/{booking.Id}");
        booking.Links.ShouldNotBeNull().ShouldHaveSingleItem(link => link.Rel == "self")
            .Href.ShouldBe(bookingResponse.Headers.Location.OriginalString);
    }
}
