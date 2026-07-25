namespace ViajantesTurismo.Admin.IntegrationTests.Customers;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.IntegrationScope)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.AspireHost)]
public sealed class CustomerApiAuthorizationTests(ApiFixture fixture)
{
    [Fact]
    public async Task Authenticated_operator_can_read_tours_but_cannot_read_customer_pii()
    {
        // Arrange
        using var client = await fixture.CreateOperatorClient(TestContext.Current.CancellationToken);
        var customerId = Guid.Parse("019f95ee-9237-7a27-a456-1b578bd5fb6d");

        // Act
        using var toursResponse = await client.GetAsync(
            new Uri("/api/v1/tours/", UriKind.Relative),
            TestContext.Current.CancellationToken);
        using var customersResponse = await client.GetAsync(
            new Uri("/api/v1/customers/", UriKind.Relative),
            TestContext.Current.CancellationToken);
        using var customerResponse = await client.GetAsync(
            new Uri($"/api/v1/customers/{customerId}", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var customersBody = await customersResponse.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        var customerBody = await customerResponse.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        // Assert
        toursResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        customersResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        customerResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        customersBody.ShouldBeEmpty();
        customerBody.ShouldBeEmpty();
    }
}
