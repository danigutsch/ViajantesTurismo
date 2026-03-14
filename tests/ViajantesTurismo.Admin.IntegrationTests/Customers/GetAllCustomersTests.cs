namespace ViajantesTurismo.Admin.IntegrationTests.Customers;

public sealed class GetAllCustomersTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
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

        var createdIds = new HashSet<Guid>
        {
            customer1.Id,
            customer2.Id,
            customer3.Id,
        };

        var createdCustomers = customers.Where(c => createdIds.Contains(c.Id)).ToArray();

        Assert.Equal(3, createdCustomers.Length);
        Assert.Contains(createdCustomers, c => c.Id == customer1.Id);
        Assert.Contains(createdCustomers, c => c.Id == customer2.Id);
        Assert.Contains(createdCustomers, c => c.Id == customer3.Id);
    }
}

public sealed class GetAllCustomersEmptyListTests(ApiFixture fixture) : AdminApiSerialTestBase(fixture)
{
    [Fact]
    [Trait("SeedDependency", "Intentional-EmptyState-Smoke")]
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
}
