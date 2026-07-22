using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViajantesTurismo.Admin.Application.Tours.CreateTour;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.DependencyInjectionCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, SharedKernel.Testing.TestTraitValues.TestServerHost)]
public sealed class AdminEndpointDependencyCompositionTests
{
    [Fact]
    public async Task Mapped_admin_mutation_endpoint_dependencies_resolve_from_the_composed_host()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create();

        // Assert
        AdminApiTestHost.VerifyMappedMutationDependencies(factory);
    }

    [Fact]
    public async Task Mapped_document_endpoint_dependencies_resolve_from_the_composed_host()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create();

        // Assert
        AdminApiTestHost.VerifyMappedDocumentDependencies(factory);
    }

    [Fact]
    public async Task Missing_create_tour_handler_causes_the_create_tour_endpoint_to_fail()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create(services => services.RemoveAll<CreateTourCommandHandler>());
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        using var content = JsonContent.Create(new CreateTourDto
        {
            Identifier = "composition-tour",
            Name = "Composition tour",
            StartDate = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2030, 1, 8, 0, 0, 0, DateTimeKind.Utc),
            Price = 1m,
            SingleRoomSupplementPrice = 1m,
            RegularBikePrice = 1m,
            EBikePrice = 1m,
            Currency = CurrencyDto.UsDollar,
            IncludedServices = ["Hotel"],
            MinCustomers = 1,
            MaxCustomers = 1
        });

        // Act
        using var response = await client.PostAsync(
            new Uri("/api/v1/tours/", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }
}
