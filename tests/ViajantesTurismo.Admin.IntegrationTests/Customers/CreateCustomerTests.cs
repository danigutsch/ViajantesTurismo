using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Helpers;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Customers;

public sealed class CreateCustomerTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Create_Customer()
    {
        // Arrange
        var request = DtoBuilders.BuildCreateCustomerDto(
            firstName: "John",
            lastName: "Doe");

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/customers", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var customer = await response.Content.ReadFromJsonAsync<GetCustomerDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(customer);
        Assert.Equal(request.PersonalInfo.FirstName, customer.FirstName);
        Assert.Equal(request.PersonalInfo.LastName, customer.LastName);
    }
}
