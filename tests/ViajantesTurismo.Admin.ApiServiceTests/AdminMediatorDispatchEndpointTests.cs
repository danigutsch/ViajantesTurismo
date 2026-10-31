using System.Net.Http.Json;
using ViajantesTurismo.Admin.Testing.Builders;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.EndpointCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, PandaBear.Testing.TestTraitValues.TestServerHost)]
public sealed class AdminMediatorDispatchEndpointTests
{
    [Theory]
    [InlineData("POST", "cancel")]
    [InlineData("POST", "complete")]
    [InlineData("POST", "confirm")]
    [InlineData("DELETE", "")]
    [InlineData("PUT", "discount")]
    [InlineData("PUT", "details")]
    [InlineData("PATCH", "notes")]
    public async Task An_empty_booking_id_is_rejected_by_the_mediator_pipeline_without_saving(string method, string action)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(action);
        var unitOfWork = new FakeUnitOfWork();
        await using var factory = AdminMediatorDispatchEndpointTestsHelpers.CreateHost(new FakeTourStore(), unitOfWork);
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        using var request = AdminMediatorDispatchEndpointTestsHelpers.CreateBookingRequest(method, Guid.Empty, action);

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Theory]
    [InlineData("POST", "cancel")]
    [InlineData("POST", "complete")]
    [InlineData("POST", "confirm")]
    [InlineData("DELETE", "")]
    [InlineData("PUT", "discount")]
    [InlineData("PUT", "details")]
    [InlineData("PATCH", "notes")]
    public async Task An_unknown_booking_id_reaches_the_handler_and_is_not_found_without_saving(string method, string action)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(action);
        var unitOfWork = new FakeUnitOfWork();
        await using var factory = AdminMediatorDispatchEndpointTestsHelpers.CreateHost(new FakeTourStore(), unitOfWork);
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        using var request = AdminMediatorDispatchEndpointTestsHelpers.CreateBookingRequest(method, Guid.CreateVersion7(), action);

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Creating_a_booking_for_an_unknown_tour_is_not_found_through_the_mediator()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        await using var factory = AdminMediatorDispatchEndpointTestsHelpers.CreateHost(new FakeTourStore(), unitOfWork);
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/bookings/", UriKind.Relative),
            DtoBuilders.BuildCreateBookingDto(),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Creating_a_tour_with_invalid_capacity_is_rejected_by_the_domain_through_the_mediator()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        await using var factory = AdminMediatorDispatchEndpointTestsHelpers.CreateHost(new FakeTourStore(), unitOfWork);
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/tours/", UriKind.Relative),
            DtoBuilders.BuildCreateTourDto(minCustomers: 10, maxCustomers: 5),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Updating_an_unknown_tour_is_not_found_through_the_mediator()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        await using var factory = AdminMediatorDispatchEndpointTestsHelpers.CreateHost(new FakeTourStore(), unitOfWork);
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/tours/{Guid.CreateVersion7()}", UriKind.Relative),
            DtoBuilders.BuildUpdateTourDto(),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }
}
