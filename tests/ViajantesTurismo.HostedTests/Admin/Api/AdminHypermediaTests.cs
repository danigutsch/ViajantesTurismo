using System.Net;
using System.Text.Json;
using SharedKernel.Testing;
using SharedKernel.Testing.Http;
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
        list.GetProperty("href").GetString().ShouldBe($"/api/v1/{collection}");
        list.GetProperty("method").GetString().ShouldBe("GET");
    }

    [Theory]
    [InlineData("customers")]
    [InlineData("bookings")]
    [InlineData("tours")]
    public async Task List_retains_its_array_shape_and_links_to_each_resource(string collection)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var created = await AdminHypermediaTestRequests.Create(fixture.Client, collection, ct);
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var createdBody = await created.Content.ReadFromJsonAsync<JsonElement>(ct);
        var id = createdBody.GetProperty("id").GetGuid();
        var location = created.Headers.Location.ShouldNotBeNull();

        // Act
        using var response = await fixture.Client.GetAsync(new Uri($"/api/v1/{collection}", UriKind.Relative), ct);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ValueKind.ShouldBe(JsonValueKind.Array);
        body.ShouldAllHaveLink("self");
        var item = body.EnumerateArray().ShouldHaveSingleItem(row => row.GetProperty("id").GetGuid() == id);
        var self = item.ShouldHaveLink("self");
        self.GetProperty("href").GetString().ShouldBe(location.OriginalString);
        self.GetProperty("method").GetString().ShouldBe("GET");
    }

    [Theory]
    [InlineData("tour", "tourId")]
    [InlineData("customer", "customerId")]
    public async Task Filtered_booking_lists_link_to_the_same_item_resource(string filter, string idProperty)
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
            new Uri($"/api/v1/bookings/{filter}/{filterId}", UriKind.Relative), ct);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var booking = body.EnumerateArray().ShouldHaveSingleItem();
        var self = booking.ShouldHaveLink("self");
        self.GetProperty("href").GetString().ShouldBe(location.OriginalString);
    }
}
