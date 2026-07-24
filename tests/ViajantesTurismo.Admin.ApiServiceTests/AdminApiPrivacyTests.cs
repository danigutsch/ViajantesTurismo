using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Tours.CreateTour;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Builders;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.SecurityCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, SharedKernel.Testing.TestTraitValues.TestServerHost)]
public sealed class AdminApiPrivacyTests
{
    [Fact]
    public async Task Duplicate_email_conflict_does_not_return_the_submitted_email()
    {
        // Arrange
        const string email = "duplicate.traveler@example.com";
        await using var factory = AdminApiTestHost.Create(services =>
        {
            services.Replace(ServiceDescriptor.Scoped<ICustomerStore>(_ => new FakeCustomerStore([email])));
            services.Replace(ServiceDescriptor.Scoped<IUnitOfWork, FakeUnitOfWork>());
        });
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var request = DtoBuilders.BuildCreateCustomerDto(email: email);

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/customers/", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problem.ShouldNotBeNull();
        problem.Detail.ShouldContain("email", StringComparison.OrdinalIgnoreCase);
        body.ShouldNotContain(email, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_tour_duplicate_identifier_conflict_does_not_reflect_the_identifier()
    {
        // Arrange
        const string identifier = "private-customer-123-tour";
        var tourStore = new FakeTourStore();
        tourStore.AddExistingTour(AdminApiPrivacyTestData.CreateTour(identifier));
        await using var factory = AdminApiTestHost.Create(services =>
        {
            services.Replace(ServiceDescriptor.Scoped<ITourStore>(_ => tourStore));
            services.Replace(ServiceDescriptor.Scoped<IUnitOfWork, FakeUnitOfWork>());
        });
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/tours/", UriKind.Relative),
            DtoBuilders.BuildCreateTourDto(identifier: identifier),
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        body.ShouldNotContain(identifier, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Update_tour_duplicate_identifier_conflict_does_not_reflect_the_identifier()
    {
        // Arrange
        const string identifier = "private-customer-123-tour";
        var tourStore = new FakeTourStore();
        var targetTour = AdminApiPrivacyTestData.CreateTour("target-tour");
        tourStore.AddExistingTour(targetTour);
        tourStore.AddExistingTour(AdminApiPrivacyTestData.CreateTour(identifier));
        await using var factory = AdminApiTestHost.Create(services =>
        {
            services.Replace(ServiceDescriptor.Scoped<ITourStore>(_ => tourStore));
            services.Replace(ServiceDescriptor.Scoped<IUnitOfWork, FakeUnitOfWork>());
        });
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/tours/{targetTour.Id}", UriKind.Relative),
            DtoBuilders.BuildUpdateTourDto(identifier: identifier),
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        body.ShouldNotContain(identifier, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Production_unhandled_error_response_excludes_input_exception_and_stack_details()
    {
        // Arrange
        const string sensitiveIdentifier = "customer-0198-booking-7f31-private";
        await using var factory = AdminApiTestHost.Create(
            services => services.RemoveAll<CreateTourCommandHandler>(),
            environment: Environments.Production);
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var request = DtoBuilders.BuildCreateTourDto(identifier: sensitiveIdentifier);

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/tours/", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        body.ShouldNotContain(sensitiveIdentifier, StringComparison.OrdinalIgnoreCase);
        body.ShouldNotContain(nameof(CreateTourCommandHandler), StringComparison.Ordinal);
        body.ShouldNotContain("System.InvalidOperationException", StringComparison.Ordinal);
        body.ShouldNotContain(" at ", StringComparison.Ordinal);
    }
}
