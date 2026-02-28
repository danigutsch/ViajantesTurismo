using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Helpers;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Customers;

public sealed class GetAllCustomersTests(ApiFixture fixture) : AdminApiSerialTestBase(fixture)
{
    [Fact]
    public async Task Can_Get_Empty_Customer_List()
    {
        // Arrange
        await ClearDatabaseAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetAllCustomersAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var customers = await response.Content.ReadFromJsonAsync<GetCustomerDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(customers);
        Assert.Empty(customers);
    }

    [Fact]
    public async Task Can_Get_Multiple_Customers()
    {
        // Arrange
        var customer1 = await Client.CreateTestCustomer("Alice", "Johnson", cancellationToken: TestContext.Current.CancellationToken);
        var customer2 = await Client.CreateTestCustomer("Bob", "Smith", cancellationToken: TestContext.Current.CancellationToken);
        var customer3 = await Client.CreateTestCustomer("Charlie", "Brown", cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetAllCustomersAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var customers = await response.Content.ReadFromJsonAsync<GetCustomerDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(customers);
        Assert.True(customers.Length >= 3);
        Assert.Contains(customers, c => c.Id == customer1.Id);
        Assert.Contains(customers, c => c.Id == customer2.Id);
        Assert.Contains(customers, c => c.Id == customer3.Id);
    }
}
