using ViajantesTurismo.Admin.SystemTests.Infrastructure;

namespace ViajantesTurismo.Admin.SystemTests.PostTransportValidation;

[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.PostTransportArea)]
[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.IntegrationEventTransportCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.SystemScope)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.AspireHost)]
[Trait(SharedKernel.Testing.TestTraitNames.SurfaceName, TestTraits.AdminSurface)]
[Trait(SharedKernel.Testing.TestTraitNames.SurfaceName, TestTraits.CatalogSurface)]
[Trait(SharedKernel.Testing.TestTraitNames.SurfaceName, TestTraits.PublicWebSurface)]
public sealed class PostTransportValidationTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Catalog_api_exposes_tour_after_admin_event_is_delivered()
    {
        // Arrange
        var scenario = new PostTransportValidationScenario(ApiClient, Fixture.CatalogTours);
        var identifier = $"PTV-{Guid.CreateVersion7():N}"[..16];
        var title = $"Transport Tour {Guid.CreateVersion7():N}"[..30];

        // Act
        var adminTour = await scenario.CreateAdminTour(identifier, title);
        var catalogTour = await scenario.WaitForCatalogTour(adminTour.Id, TestContext.Current.CancellationToken);

        // Assert
        catalogTour.AdminTourId.ShouldBe(adminTour.Id);
        catalogTour.Identifier.ShouldBe(identifier);
        catalogTour.Title.ShouldBe(title);
        catalogTour.IsPublished.ShouldBeFalse();
    }

    [Fact]
    public async Task Public_web_renders_published_tour_from_real_catalog_api()
    {
        // Arrange
        var scenario = new PostTransportValidationScenario(ApiClient, Fixture.CatalogTours);
        var unique = Guid.CreateVersion7().ToString("N")[..12];
        var identifier = $"PUB-{unique}";
        var title = $"Published Tour {unique}";
        var slug = $"published-tour-{unique}";
        var adminTour = await scenario.CreateAdminTour(identifier, title);
        var catalogTour = await scenario.WaitForCatalogTour(adminTour.Id, TestContext.Current.CancellationToken);
        var published = await scenario.PublishCatalogTour(catalogTour, title, slug, TestContext.Current.CancellationToken);

        // Act
        await NavigateTo(new Uri(Fixture.PublicWebAppUrl, "/group-bike-tours"));

        // Assert
        published.IsPublished.ShouldBeTrue();
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Group Bike Tours" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = title })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Admin_api_to_public_flow_renders_tour_details_after_transport_projection()
    {
        // Arrange
        var scenario = new PostTransportValidationScenario(ApiClient, Fixture.CatalogTours);
        var unique = Guid.CreateVersion7().ToString("N")[..12];
        var identifier = $"E2E-{unique}";
        var title = $"End To End Tour {unique}";
        var slug = $"end-to-end-tour-{unique}";

        // Act
        var adminTour = await scenario.CreateAdminTour(identifier, title);
        var catalogTour = await scenario.WaitForCatalogTour(adminTour.Id, TestContext.Current.CancellationToken);
        var published = await scenario.PublishCatalogTour(catalogTour, title, slug, TestContext.Current.CancellationToken);
        await NavigateTo(new Uri(Fixture.PublicWebAppUrl, $"/group-bike-tours/{published.Slug}"));

        // Assert
        published.AdminTourId.ShouldBe(adminTour.Id);
        published.Identifier.ShouldBe(identifier);
        published.Slug.ShouldBe(slug);
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = title })).ToBeVisibleAsync();
        await Expect(Page.GetByText($"Discover {title} by bicycle.")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Duplicate_transport_delivery_does_not_duplicate_catalog_result()
    {
        // Arrange
        var scenario = new PostTransportValidationScenario(ApiClient, Fixture.CatalogTours);
        var unique = Guid.CreateVersion7().ToString("N")[..12];
        var identifier = $"DUP-{unique}";
        var title = $"Duplicate Tour {unique}";
        var slug = $"duplicate-tour-{unique}";
        var adminTour = await scenario.CreateAdminTour(identifier, title);
        var catalogTour = await scenario.WaitForCatalogTour(adminTour.Id, TestContext.Current.CancellationToken);
        var eventCountBeforeDuplicate = await Fixture.CountCatalogTourEvents(adminTour.Id, TestContext.Current.CancellationToken);

        // Act
        await Fixture.RequeueCatalogTransportMessageForAdminTour(adminTour.Id, TestContext.Current.CancellationToken);
        await Fixture.WaitForCatalogTransportMessageProcessed(adminTour.Id, TestContext.Current.CancellationToken);
        var catalogTours = await Fixture.CatalogTours.GetTours(TestContext.Current.CancellationToken);
        var eventCountAfterDuplicate = await Fixture.CountCatalogTourEvents(adminTour.Id, TestContext.Current.CancellationToken);
        await scenario.PublishCatalogTour(catalogTour, title, slug, TestContext.Current.CancellationToken);
        await NavigateTo(new Uri(Fixture.PublicWebAppUrl, "/group-bike-tours"));

        // Assert
        eventCountBeforeDuplicate.ShouldBe(1);
        eventCountAfterDuplicate.ShouldBe(1);
        var projectedTours = catalogTours.Where(tour => tour.AdminTourId == adminTour.Id).ToArray();
        projectedTours.ShouldHaveSingleItem().Id.ShouldBe(catalogTour.Id);
        await Expect(Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = title })).ToHaveCountAsync(1);
    }
}
