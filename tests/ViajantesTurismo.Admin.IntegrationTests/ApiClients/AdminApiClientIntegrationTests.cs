using Microsoft.Extensions.Logging.Abstractions;

namespace ViajantesTurismo.Admin.IntegrationTests.ApiClients;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SmokeCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.IntegrationScope)]
public sealed class AdminApiClientIntegrationTests(AspireSerialIntegrationTestFixture fixture)
    : AspireSerialIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Customers_client_creates_and_reads_customer_through_api_host()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var sut = new CustomersApiClient(Client, NullLogger<CustomersApiClient>.Instance);
        var request = DtoBuilders.BuildCreateCustomerDto(firstName: "Contract", lastName: "Client");

        // Act
        var outcome = await sut.CreateCustomer(request, cancellationToken);

        // Assert
        Assert.Equal(CustomerCreateOutcomeKind.Succeeded, outcome.Kind);
        Assert.Equal(HttpStatusCode.Created, outcome.StatusCode);
        Assert.NotNull(outcome.Location);

        var customerIdText = outcome.Location.OriginalString.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];
        Assert.True(Guid.TryParse(customerIdText, out var customerId));

        var customer = await sut.GetCustomerById(customerId, cancellationToken);
        Assert.NotNull(customer);
        Assert.Equal("Contract", customer.PersonalInfo.FirstName);
        Assert.Equal("Client", customer.PersonalInfo.LastName);
    }

    [Fact]
    public async Task Tours_client_creates_updates_and_reads_tour_through_api_host()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var sut = new ToursApiClient(Client);
        var createRequest = DtoBuilders.BuildCreateTourDto(name: "Contract Client Tour");

        // Act
        var location = await sut.CreateTour(createRequest, cancellationToken);

        // Assert
        var tourIdText = location.OriginalString.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];
        Assert.True(Guid.TryParse(tourIdText, out var tourId));

        var created = await sut.GetTourById(tourId, cancellationToken);
        Assert.NotNull(created);
        Assert.Equal("Contract Client Tour", created.Name);

        var updateRequest = DtoBuilders.BuildUpdateTourDto(identifier: created.Identifier, name: "Updated Contract Client Tour");
        await sut.UpdateTour(tourId, updateRequest, cancellationToken);

        var updated = await sut.GetTourById(tourId, cancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("Updated Contract Client Tour", updated.Name);
    }
}
