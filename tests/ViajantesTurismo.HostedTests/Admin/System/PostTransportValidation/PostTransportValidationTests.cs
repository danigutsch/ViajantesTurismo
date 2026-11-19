using System.Net;
using PandaBear.Testing;
using Microsoft.Playwright;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Bases;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Fixtures;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Helpers;
using PandaBear.HttpClients;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure;
using ViajantesTurismo.Catalog.Contracts.Http;

namespace ViajantesTurismo.HostedTests.Admin.System.PostTransportValidation;

[Trait(TestTraitNames.AreaName, TestTraits.PostTransportArea)]
[Trait(TestTraitNames.CategoryName, TestTraits.IntegrationEventTransportCategory)]
[Trait(TestTraitNames.HostName, TestTraits.AspireHost)]
[Trait(TestTraitNames.SurfaceName, TestTraits.AdminSurface)]
[Trait(TestTraitNames.SurfaceName, TestTraits.CatalogSurface)]
[Trait(TestTraitNames.SurfaceName, TestTraits.PublicWebSurface)]
[TestScope(TestScope.System)]
public sealed class PostTransportValidationTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Acknowledged_tour_retirement_withdraws_the_warmed_public_list_and_detail()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var catalogApiClient = await Fixture.CreateCatalogApiClient(ct);
        var catalogTours = new CatalogToursApiClient(catalogApiClient);
        var scenario = new PostTransportValidationScenario(ApiClient, catalogTours);
        var unique = Guid.CreateVersion7().ToString("N")[..12];
        var title = $"Retiring Tour {unique}";
        var publicList = new Uri(Fixture.PublicWebAppUrl, "/en/group-bike-tours");
        var adminTour = await scenario.CreateAdminTour($"RET-{unique}", title);
        var catalogTour = await scenario.WaitForCatalogTour(adminTour.Id, ct);
        var published = await scenario.PublishCatalogTour(catalogTour, title, $"retiring-tour-{unique}", ct);
        await NavigateTo(publicList);
        var tourLink = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = title });
        await Expect(tourLink).ToBeVisibleAsync();
        var publicDetail = new Uri(publicList, $"/en/group-bike-tours/{published.Slug}");
        await NavigateTo(publicDetail);
        var tourHeading = Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = title });
        await Expect(tourHeading).ToBeVisibleAsync();

        // Act
        var retirementPath = new Uri($"/api/v1/tours/{adminTour.Id:D}", UriKind.Relative);
        using var initialResult = await ApiClient.DeleteAsync(retirementPath, ct);

        // Assert
        initialResult.StatusCode.ShouldBeOneOf(HttpStatusCode.Accepted, HttpStatusCode.NoContent);
        if (initialResult.StatusCode == HttpStatusCode.Accepted)
        {
            initialResult.Headers.Location.ShouldBe(new Uri($"{retirementPath}/retirement", UriKind.Relative));
        }

        await scenario.WaitForRetirement(adminTour.Id, ct);
        await NavigateTo(publicList);
        await Expect(tourLink).ToHaveCountAsync(0);
        var retiredDetail = await Page.GotoAsync(publicDetail.ToString());
        retiredDetail.ShouldNotBeNull().Status.ShouldBe((int)HttpStatusCode.NotFound);
        await Expect(tourHeading).ToHaveCountAsync(0);
        var retainedTour = await catalogTours.GetTour(catalogTour.Id, ct);
        retainedTour.Value.ShouldNotBeNull().IsPublished.ShouldBeFalse();
    }

    [Fact]
    public async Task Catalog_API_exposes_tour_after_admin_event_is_delivered()
    {
        // Arrange
        using var catalogApiClient = await Fixture.CreateCatalogApiClient(TestContext.Current.CancellationToken);
        var catalogTours = new CatalogToursApiClient(catalogApiClient);
        var scenario = new PostTransportValidationScenario(ApiClient, catalogTours);
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
    public async Task Public_web_renders_published_tour_from_real_catalog_API()
    {
        // Arrange
        using var catalogApiClient = await Fixture.CreateCatalogApiClient(TestContext.Current.CancellationToken);
        var catalogTours = new CatalogToursApiClient(catalogApiClient);
        var scenario = new PostTransportValidationScenario(ApiClient, catalogTours);
        var unique = Guid.CreateVersion7().ToString("N")[..12];
        var identifier = $"PUB-{unique}";
        var title = $"Published Tour {unique}";
        var slug = $"published-tour-{unique}";
        var adminTour = await scenario.CreateAdminTour(identifier, title);
        var catalogTour = await scenario.WaitForCatalogTour(adminTour.Id, TestContext.Current.CancellationToken);
        var published = await scenario.PublishCatalogTour(catalogTour, title, slug, TestContext.Current.CancellationToken);

        // Act
        await NavigateTo(new Uri(Fixture.PublicWebAppUrl, "/en/group-bike-tours"));

        // Assert
        published.IsPublished.ShouldBeTrue();
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Group Bike Tours" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = title })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Admin_API_to_public_flow_renders_tour_details_after_transport_projection()
    {
        // Arrange
        using var catalogApiClient = await Fixture.CreateCatalogApiClient(TestContext.Current.CancellationToken);
        var catalogTours = new CatalogToursApiClient(catalogApiClient);
        var scenario = new PostTransportValidationScenario(ApiClient, catalogTours);
        var unique = Guid.CreateVersion7().ToString("N")[..12];
        var identifier = $"E2E-{unique}";
        var title = $"End To End Tour {unique}";
        var slug = $"end-to-end-tour-{unique}";

        // Act
        var adminTour = await scenario.CreateAdminTour(identifier, title);
        var catalogTour = await scenario.WaitForCatalogTour(adminTour.Id, TestContext.Current.CancellationToken);
        var published = await scenario.PublishCatalogTour(catalogTour, title, slug, TestContext.Current.CancellationToken);
        await NavigateTo(new Uri(Fixture.PublicWebAppUrl, $"/pt/group-bike-tours/{published.Slug}"));

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
        using var catalogApiClient = await Fixture.CreateCatalogApiClient(TestContext.Current.CancellationToken);
        var catalogTours = new CatalogToursApiClient(catalogApiClient);
        var scenario = new PostTransportValidationScenario(ApiClient, catalogTours);
        var unique = Guid.CreateVersion7().ToString("N")[..12];
        var identifier = $"DUP-{unique}";
        var title = $"Duplicate Tour {unique}";
        var adminTour = await scenario.CreateAdminTour(identifier, title);
        var catalogTour = await scenario.WaitForCatalogTour(adminTour.Id, TestContext.Current.CancellationToken);
        var eventCountBeforeDuplicate = await Fixture.CountCatalogTourEvents(adminTour.Id, TestContext.Current.CancellationToken);

        // Act
        await Fixture.RequeueCatalogTransportMessageForAdminTour(adminTour.Id, TestContext.Current.CancellationToken);
        await Fixture.WaitForCatalogTransportMessageProcessed(adminTour.Id, TestContext.Current.CancellationToken);
        var projectedCatalogToursOutcome = await catalogTours.GetTours(TestContext.Current.CancellationToken);
        var eventCountAfterDuplicate = await Fixture.CountCatalogTourEvents(adminTour.Id, TestContext.Current.CancellationToken);

        // Assert
        eventCountBeforeDuplicate.ShouldBe(1);
        eventCountAfterDuplicate.ShouldBe(1);
        projectedCatalogToursOutcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var projectedCatalogTours = projectedCatalogToursOutcome.Value.ShouldNotBeNull();
        var projectedTours = projectedCatalogTours.Where(tour => tour.AdminTourId == adminTour.Id).ToArray();
        projectedTours.ShouldHaveSingleItem().Id.ShouldBe(catalogTour.Id);
    }
}
