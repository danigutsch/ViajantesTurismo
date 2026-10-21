using Microsoft.Extensions.Logging.Abstractions;
using PandaBear.HttpClients;

namespace ViajantesTurismo.Admin.IntegrationTests.ApiClients;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.SmokeCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.IntegrationScope)]
public sealed class AdminApiClientIntegrationTests(ApiFixture fixture)
{
    [Fact]
    public async Task Customers_client_creates_and_reads_customer_through_api_host()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var sut = new CustomersApiClient(fixture.Client, NullLogger<CustomersApiClient>.Instance);
        var request = DtoBuilders.BuildCreateCustomerDto(firstName: "Contract", lastName: "Client");

        // Act
        var outcome = await sut.CreateCustomer(request, cancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        outcome.StatusCode.ShouldBe(HttpStatusCode.Created);
        outcome.Location.ShouldNotBeNull();

        var customerIdText = outcome.Location.OriginalString.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];
        Guid.TryParse(customerIdText, out var customerId).ShouldBeTrue();

        var customerOutcome = await sut.GetCustomerById(customerId, cancellationToken);
        customerOutcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var customer = customerOutcome.Value.ShouldNotBeNull();
        customer.PersonalInfo.FirstName.ShouldBe("Contract");
        customer.PersonalInfo.LastName.ShouldBe("Client");
    }

    [Fact]
    public async Task Tours_client_creates_updates_and_reads_tour_through_api_host()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var sut = new ToursApiClient(fixture.Client);
        var createRequest = DtoBuilders.BuildCreateTourDto(name: "Contract Client Tour");

        // Act
        var outcome = await sut.CreateTour(createRequest, cancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        outcome.Location.ShouldNotBeNull();
        var tourIdText = outcome.Location.OriginalString.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];
        Guid.TryParse(tourIdText, out var tourId).ShouldBeTrue();

        var createdOutcome = await sut.GetTourById(tourId, cancellationToken);
        createdOutcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var created = createdOutcome.Value.ShouldNotBeNull();
        created.Name.ShouldBe("Contract Client Tour");

        var updateRequest = DtoBuilders.BuildUpdateTourDto(identifier: created.Identifier, name: "Updated Contract Client Tour");
        var updateOutcome = await sut.UpdateTour(tourId, updateRequest, cancellationToken);

        var updatedOutcome = await sut.GetTourById(tourId, cancellationToken);
        updateOutcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        updatedOutcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var updated = updatedOutcome.Value.ShouldNotBeNull();
        updated.Name.ShouldBe("Updated Contract Client Tour");
    }

    [Fact]
    public async Task Bookings_client_updates_notes_through_api_host()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("booking-client-notes", "Booking client notes", cancellationToken);
        var customer = await fixture.Client.CreateTestCustomer("Booking", "Client", cancellationToken);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, companionId: null, ct: cancellationToken);
        var sut = new BookingsApiClient(fixture.Client);

        // Act
        var updateOutcome = await sut.UpdateBookingNotes(
            booking.Id,
            DtoBuilders.BuildUpdateBookingNotesDto("Updated through the typed client"),
            cancellationToken);
        var readOutcome = await sut.GetBookingById(booking.Id, cancellationToken);

        // Assert
        updateOutcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        updateOutcome.StatusCode.ShouldBe(HttpStatusCode.OK);
        readOutcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var updated = readOutcome.Value.ShouldNotBeNull();
        updated.Notes.ShouldBe("Updated through the typed client");
    }
}
