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
        outcome.Kind.ShouldBe(CustomerCreateOutcomeKind.Succeeded);
        outcome.StatusCode.ShouldBe(HttpStatusCode.Created);
        outcome.Location.ShouldNotBeNull();

        var customerIdText = outcome.Location.OriginalString.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];
        Guid.TryParse(customerIdText, out var customerId).ShouldBeTrue();

        var customer = await sut.GetCustomerById(customerId, cancellationToken);
        customer.ShouldNotBeNull();
        customer.PersonalInfo.FirstName.ShouldBe("Contract");
        customer.PersonalInfo.LastName.ShouldBe("Client");
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
        Guid.TryParse(tourIdText, out var tourId).ShouldBeTrue();

        var created = await sut.GetTourById(tourId, cancellationToken);
        created.ShouldNotBeNull();
        created.Name.ShouldBe("Contract Client Tour");

        var updateRequest = DtoBuilders.BuildUpdateTourDto(identifier: created.Identifier, name: "Updated Contract Client Tour");
        await sut.UpdateTour(tourId, updateRequest, cancellationToken);

        var updated = await sut.GetTourById(tourId, cancellationToken);
        updated.ShouldNotBeNull();
        updated.Name.ShouldBe("Updated Contract Client Tour");
    }
}
