using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Customers;

public sealed class GetAllCustomersTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Get_Customers()
    {
        // Act
        var response = await Client.GetAsync(new Uri("/customers", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var customers = await response.Content.ReadFromJsonAsync<GetCustomerDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(customers);
        Assert.NotEmpty(customers);
    }
}
