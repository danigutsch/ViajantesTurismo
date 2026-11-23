using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.EndpointCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, PandaBear.Testing.TestTraitValues.TestServerHost)]
public sealed class TourOptionsEndpointTests
{
    [Theory]
    [InlineData("Reader")]
    [InlineData("Operator")]
    public async Task Authorized_tour_reader_can_request_bounded_active_tour_choices(string role)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (testHost, queries) = AdminHypermediaPathBase.CreateHost();
        await using var factory = testHost;
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, role);

        // Act
        using var query = JsonContent.Create(new TourOptionSearchDto("serra", 10));
        using var response = await client.PostAsync(
            new Uri($"{AdminHypermediaPathBase.Prefix}/api/v1/tours/options/search", UriKind.Relative), query, ct);
        var result = await response.Content.ReadFromJsonAsync<GetTourOptionsDto>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var choices = result.ShouldNotBeNull();
        choices.Total.ShouldBe(1);
        choices.Items.ShouldHaveSingleItem()
            .ShouldBe(new GetTourOptionDto(queries.Tour.Id, queries.Tour.Name, queries.Tour.Identifier, queries.Tour.StartDate));
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Unrelated", HttpStatusCode.Forbidden)]
    public async Task Tour_search_requires_existing_tour_read_access(string? role, HttpStatusCode status)
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
        using var query = JsonContent.Create(new TourOptionSearchDto(null, 10));
        using var response = await client.PostAsync(new Uri("/api/v1/tours/options/search", UriKind.Relative), query, ct);

        // Assert
        response.StatusCode.ShouldBe(status);
    }

    [Theory]
    [InlineData(201, 10)]
    [InlineData(0, 0)]
    [InlineData(0, -1)]
    public async Task Rejects_invalid_search_bounds(int queryLength, int limit)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Reader");

        // Act
        using var query = JsonContent.Create(new TourOptionSearchDto(new string('x', queryLength), limit));
        using var response = await client.PostAsync(new Uri("/api/v1/tours/options/search", UriKind.Relative), query, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
