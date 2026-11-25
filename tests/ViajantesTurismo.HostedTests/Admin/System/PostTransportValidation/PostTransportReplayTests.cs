using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Contracts.Http;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Fixtures;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Helpers;

namespace ViajantesTurismo.HostedTests.Admin.System.PostTransportValidation;

/// <summary>Exercises duplicate transport delivery through the hosted Admin and Catalog stack.</summary>
[Trait(TestTraitNames.AreaName, TestTraits.PostTransportArea)]
[Trait(TestTraitNames.CategoryName, TestTraitValues.IntegrationEventTransport)]
[TestHost(TestHost.Aspire)]
[Trait(TestTraitNames.SurfaceName, TestTraits.AdminSurface)]
[Trait(TestTraitNames.SurfaceName, TestTraits.CatalogSurface)]
[TestScope(TestScope.System)]
public sealed class PostTransportReplayTests(AspireSystemTestFixture fixture) : IClassFixture<AspireSystemTestFixture>
{
    [Fact]
    public async Task Duplicate_transport_delivery_does_not_duplicate_catalog_result()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var apiClient = await fixture.CreateApiClient(ct);
        using var catalogApiClient = await fixture.CreateCatalogApiClient(ct);
        var catalogTours = new CatalogToursApiClient(catalogApiClient);
        var scenario = new PostTransportValidationScenario(apiClient, catalogTours);
        var unique = Guid.CreateVersion7().ToString("N")[..12];
        var identifier = $"DUP-{unique}";
        var title = $"Duplicate Tour {unique}";
        var adminTour = await scenario.CreateAdminTour(identifier, title);
        var catalogTour = await scenario.WaitForCatalogTour(adminTour.Id, identifier, ct);
        var eventCountBeforeDuplicate = await fixture.CountCatalogTourEvents(adminTour.Id, ct);

        // Act
        await fixture.RequeueCatalogTransportMessageForAdminTour(adminTour.Id, ct);
        await fixture.WaitForCatalogTransportMessageProcessed(adminTour.Id, ct);
        var projectedTours = await scenario.FindCatalogTours(identifier, ct);
        var eventCountAfterDuplicate = await fixture.CountCatalogTourEvents(adminTour.Id, ct);

        // Assert
        eventCountBeforeDuplicate.ShouldBe(1);
        eventCountAfterDuplicate.ShouldBe(1);
        projectedTours.ShouldNotBeNull().ShouldHaveSingleItem().Id.ShouldBe(catalogTour.Id);
    }
}
