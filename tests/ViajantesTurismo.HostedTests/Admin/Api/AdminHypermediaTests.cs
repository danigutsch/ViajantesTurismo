using System.Net;
using System.Text.Json;
using SharedKernel.Testing;
using SharedKernel.Testing.Http;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.HostedTests.Admin.Api.Infrastructure;

namespace ViajantesTurismo.HostedTests.Admin.Api;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[TestScope(TestScope.Integration)]
public sealed class AdminHypermediaTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Theory]
    [InlineData("customers")]
    [InlineData("bookings")]
    [InlineData("tours")]
    public async Task Create_returns_the_same_resource_address_in_the_self_link_and_location(string collection)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await AdminHypermediaTestRequests.Create(fixture.Client, collection, ct);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var location = response.Headers.Location.ShouldNotBeNull();
        var id = body.GetProperty("id").GetGuid();
        location.OriginalString.ShouldBe($"/api/v1/{collection}/{id}");
        var self = body.ShouldHaveLink("self");
        self.GetProperty("href").GetString().ShouldBe(location.OriginalString);
        self.GetProperty("method").GetString().ShouldBe("GET");
    }

    [Theory]
    [InlineData("customers")]
    [InlineData("bookings")]
    [InlineData("tours")]
    public async Task Item_response_includes_its_canonical_address_and_collection(string collection)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var created = await AdminHypermediaTestRequests.Create(fixture.Client, collection, ct);
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var location = created.Headers.Location.ShouldNotBeNull();

        // Act
        using var response = await fixture.Client.GetAsync(location, ct);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var self = body.ShouldHaveLink("self");
        self.GetProperty("href").GetString().ShouldBe(location.OriginalString);
        self.GetProperty("method").GetString().ShouldBe("GET");
        var list = body.ShouldHaveLink("collection");
        list.GetProperty("href").GetString().ShouldBe($"/api/v1/{collection}/page");
        list.GetProperty("method").GetString().ShouldBe("GET");
    }

    [Theory]
    [InlineData("customers", ContractConstants.CustomerListPageSize, "total", JsonValueKind.Number)]
    [InlineData("bookings", ContractConstants.BookingListPageSize, "counts", JsonValueKind.Object)]
    [InlineData("tours", ContractConstants.TourListPageSize, "total", JsonValueKind.Number)]
    public async Task Collection_pages_have_bounded_items_with_canonical_links(
        string collection, int pageSize, string pagingProperty, JsonValueKind pagingKind)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var created = await AdminHypermediaTestRequests.Create(fixture.Client, collection, ct);
        created.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act
        using var response = await fixture.Client.GetAsync(new Uri($"/api/v1/{collection}/page", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        body.GetProperty(pagingProperty).ValueKind.ShouldBe(pagingKind);
        var items = body.GetProperty("items");
        items.ValueKind.ShouldBe(JsonValueKind.Array);
        var count = items.GetArrayLength();
        count.ShouldBeGreaterThan(0);
        count.ShouldBeLessThanOrEqualTo(pageSize);
        foreach (var item in items.EnumerateArray())
        {
            var id = item.GetProperty("id").GetGuid();
            var self = item.ShouldHaveLink("self");
            self.GetProperty("href").GetString().ShouldBe($"/api/v1/{collection}/{id}");
            self.GetProperty("method").GetString().ShouldBe("GET");
        }
    }

    [Theory]
    [InlineData("tour", "tourId")]
    [InlineData("customer", "customerId")]
    public async Task Filtered_booking_history_pages_link_to_the_same_item_resource(string filter, string idProperty)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var created = await AdminHypermediaTestRequests.Create(fixture.Client, "bookings", ct);
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var createdBody = await created.Content.ReadFromJsonAsync<JsonElement>(ct);
        var filterId = createdBody.GetProperty(idProperty).GetGuid();
        var location = created.Headers.Location.ShouldNotBeNull();

        // Act
        using var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/bookings/{filter}/{filterId}/page", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        body.GetProperty("total").GetInt32().ShouldBe(1);
        var booking = body.GetProperty("items").EnumerateArray().ShouldHaveSingleItem();
        var id = createdBody.GetProperty("id").GetGuid();
        booking.GetProperty("id").GetGuid().ShouldBe(id);
        var self = booking.ShouldHaveLink("self");
        self.GetProperty("href").GetString().ShouldBe(location.OriginalString);
        self.GetProperty("method").GetString().ShouldBe("GET");
    }
}
