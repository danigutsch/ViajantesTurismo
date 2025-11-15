using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Helpers;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Customers;

public sealed class GetCustomerByIdTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Get_Customer_By_Id()
    {
        // Arrange
        var customer = await Client.CreateTestCustomer("Jane", "Smith", TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetAsync(new Uri($"/customers/{customer.Id}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var customerDto = await response.Content.ReadFromJsonAsync<CustomerDetailsDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(customerDto);
        Assert.Equal(customer.Id, customerDto.Id);
        Assert.Equal(customer.FirstName, customerDto.PersonalInfo.FirstName);
        Assert.Equal(customer.LastName, customerDto.PersonalInfo.LastName);
    }

    [Fact]
    public async Task Get_Customer_By_Id_Returns_Not_Found_For_Invalid_Id()
    {
        // Arrange
        const int invalidId = -1;

        // Act
        var response = await Client.GetAsync(new Uri($"/customers/{invalidId}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
