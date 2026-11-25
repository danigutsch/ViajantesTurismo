using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing.Builders;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, SharedKernel.Testing.TestTraitValues.TestServerHost)]
public sealed class TourPageEndpointTests
{
    [Theory]
    [InlineData("Admin")]
    [InlineData("Operator")]
    [InlineData("Reader")]
    public async Task Tour_read_roles_can_read_the_overview_page(string role)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (testHost, queries) = AdminHypermediaPathBase.CreateHost();
        await using var factory = testHost;
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, role);

        // Act
        using var response = await client.GetAsync(
            new Uri($"{AdminHypermediaPathBase.Prefix}/api/v1/tours/page?page=2&sort=price&descending=true", UriKind.Relative), ct);
        var result = await response.Content.ReadFromJsonAsync<GetTourPageDto>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = result.ShouldNotBeNull();
        page.Total.ShouldBe(1);
        var expected = DtoBuilders.ToTourListItem(queries.Tour);
        var row = page.Items.ShouldHaveSingleItem();
        var fields = row with
        {
            Links = null
        };
        fields.ShouldBe(expected);
        row.Links.ShouldNotBeNull().ShouldHaveSingleItem(link => link.Rel == "collection")
            .Href.ShouldBe($"{AdminHypermediaPathBase.Prefix}/api/v1/tours/page");
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Unrelated", HttpStatusCode.Forbidden)]
    public async Task A_tour_page_requires_the_existing_read_grant(string? role, HttpStatusCode status)
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
        using var response = await client.GetAsync(new Uri("/api/v1/tours/page", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(status);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("99")]
    public async Task A_tour_page_rejects_unknown_sort_columns(string sort)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Reader");

        // Act
        using var response = await client.GetAsync(new Uri($"/api/v1/tours/page?sort={sort}", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
