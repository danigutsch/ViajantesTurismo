using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.EndpointCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, PandaBear.Testing.TestTraitValues.TestServerHost)]
public sealed class CustomerPageEndpointTests
{
    [Fact]
    public async Task Admin_can_read_a_bounded_customer_page()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var (testHost, queries) = AdminHypermediaPathBase.CreateHost();
        await using var factory = testHost;
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        using var response = await client.GetAsync(
            new Uri($"{AdminHypermediaPathBase.Prefix}/api/v1/customers/page?page=2&sort=nationality&descending=true", UriKind.Relative), ct);
        var page = await response.Content.ReadFromJsonAsync<GetCustomerPageDto>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        page.ShouldNotBeNull();
        page.Total.ShouldBe(1);
        var expected = queries.Customer;
        var row = page.Items.ShouldHaveSingleItem();
        row.ShouldBe(new GetCustomerListItemDto(expected.Id, expected.FirstName, expected.LastName,
            expected.Email, expected.Mobile, expected.Nationality));
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Operator", HttpStatusCode.Forbidden)]
    public async Task Customer_pages_require_the_existing_customer_read_grants(string? role, HttpStatusCode status)
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
        using var response = await client.GetAsync(new Uri("/api/v1/customers/page", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(status);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("99")]
    public async Task Customer_pages_reject_unknown_sort_keys(string sort)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Reader");

        // Act
        using var response = await client.GetAsync(new Uri($"/api/v1/customers/page?sort={sort}", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
