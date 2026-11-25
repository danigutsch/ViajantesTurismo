using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Api;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests.Customers;

[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraitValues.TestServerHost)]
public sealed class CustomerImportConflictEndpointTests
{
    private static readonly Uri Route = new("/api/v1/customers/import/conflicts/lookup", UriKind.Relative);

    [Fact]
    public async Task Lookup_returns_only_matching_IDs_in_request_order()
    {
        // Arrange
        var customers = new FakeCustomerStore();
        var existing = EntityBuilders.BuildCustomer(new CustomerOptions(Email: "existing@example.test"));
        customers.Add(existing);
        await using var host = AdminApiTestHost.Create(services => services.AddScoped<ICustomerStore>(_ => customers));
        using var client = host.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Reader");
        string[] keys = ["file-only@example.test", " EXISTING@EXAMPLE.TEST "];
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await client.PostAsJsonAsync(Route, keys, ct);
        var matches = await response.Content.ReadFromJsonAsync<Guid?[]>(ct);
        var endpoint = host.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => string.Equals(endpoint.RoutePattern.RawText?.TrimStart('/'), Route.OriginalString.TrimStart('/'), StringComparison.Ordinal))
            .ShouldHaveSingleItem();
        var policies = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Select(metadata => metadata.Policy);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        matches.ShouldNotBeNull().ShouldBe([null, existing.Id]);
        policies.ShouldContain(AdminAuthorization.CustomerRead);
        policies.ShouldContain(AdminAuthorization.CustomerSensitiveRead);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Operator", HttpStatusCode.Forbidden)]
    public async Task Lookup_requires_customer_read_permissions(string? role, HttpStatusCode expectedStatus)
    {
        // Arrange
        await using var host = AdminApiTestHost.Create();
        using var client = host.CreateClient();
        if (role is not null)
        {
            AdminApiTestHost.ConfigureAuthenticatedClient(client, role);
        }

        string[] keys = ["existing@example.test"];
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await client.PostAsJsonAsync(Route, keys, ct);

        // Assert
        response.StatusCode.ShouldBe(expectedStatus);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(ContractConstants.CustomerImportConflictLookupMaxKeys + 1)]
    public async Task Lookup_rejects_counts_outside_the_limit_without_echoing_keys(int count)
    {
        // Arrange
        const string email = "sensitive@example.test";
        await using var host = AdminApiTestHost.Create();
        using var client = host.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Reader");
        var keys = Enumerable.Repeat(email, count).ToArray();
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await client.PostAsJsonAsync(Route, keys, ct);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problem.ShouldNotBeNull().Errors.Keys.ShouldContain("emails");
        body.ShouldNotContain(email, StringComparison.Ordinal);
    }
}
