using Microsoft.EntityFrameworkCore;
using SharedKernel.Testing;
using SharedKernel.Testing.AspNetCore;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.Http;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure.Tours;
using ViajantesTurismo.Catalog.Testing.Infrastructure;
using ViajantesTurismo.HostedTests.Management.Browser;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Exercises the bounded Catalog list through PostgreSQL, HTTP and Management prerendering.</summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.Integration)]
public sealed class CatalogTourListPrerenderTests(HostedTestFixture fixture)
{
    [Fact]
    public async Task Large_catalog_prerenders_one_page_with_bounded_server_state()
    {
        // Arrange
        const int tourCount = 800;
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.Databases.Create(ct);
        await using var databaseContext = new CatalogDbContextDesignTimeFactory().CreateDbContext([]);
        databaseContext.Database.SetConnectionString(database.ConnectionString);
        await databaseContext.Database.MigrateAsync(ct);
        databaseContext.CatalogTourReadModels.AddRange(Enumerable.Range(0, tourCount).Select(index => new CatalogTourReadModelEntity
        {
            CatalogTourId = Guid.Parse($"00000000-0000-0000-0000-{index + 1:D12}"),
            AdminTourId = Guid.CreateVersion7(),
            Identifier = $"SSRTOUR-{index:D3}",
            Title = "Same title",
            Slug = $"ssrtour-{tourCount - 1 - index:D3}",
            IsPublished = index % 2 == 0,
            Description = new string('x', 2048),
            UpdatedAt = DateTimeOffset.UnixEpoch,
        }));
        await databaseContext.SaveChangesAsync(ct);

        var scenario = new CatalogTourListPrerenderScenario();
        await using var baseApi = CatalogApiTestHost.Create();
        await using var catalogApi = scenario.WithObservedPageReads(baseApi, database.ConnectionString);
        using var catalogHttp = catalogApi.CreateClient();
        var catalogClient = new CatalogToursApiClient(catalogHttp);
        await using var management = await ManagementWebBrowserHost.StartWithClients(ct, catalog: catalogClient);
        using var browser = new HttpClient { BaseAddress = management.BaseUri, Timeout = TimeSpan.FromSeconds(15) };
        var first = Enumerable.Range(0, 10);
        var lastReversed = Enumerable.Range(tourCount - 10, 10).Reverse();
        var bookmarkedPage = new Uri("/catalog/tours?page=2&sort=Identifier&direction=desc", UriKind.Relative);
        var expectedBookmarkedIdentifiers = Enumerable.Range(tourCount - 20, 10).Reverse().Select(index => $"{index:D3}");
        (CatalogTourListSort Sort, bool Descending, IEnumerable<int> Expected)[] orderingCases =
        [
            (CatalogTourListSort.Identifier, false, first),
            (CatalogTourListSort.Identifier, true, lastReversed),
            (CatalogTourListSort.Slug, false, lastReversed),
            (CatalogTourListSort.Slug, true, first),
            (CatalogTourListSort.IsPublished, false, first.Select(index => (2 * index) + 1)),
            (CatalogTourListSort.IsPublished, true, first.Select(index => 2 * index)),
            (CatalogTourListSort.Title, true, first),
        ];

        // Act
        var secondPage = await catalogClient.GetTourPage(2, CatalogTourListSort.Title, descending: false, ct);
        var html = await browser.GetStringAsync(new Uri("/catalog/tours", UriKind.Relative), ct);
        var firstViewReads = scenario.PageReads;
        var bookmarkedHtml = await browser.GetStringAsync(bookmarkedPage, ct);
        var bookmarkedViewReads = scenario.PageReads - firstViewReads;
        var orderedPages = new List<CatalogTourPageDto?>(orderingCases.Length);
        foreach (var (sort, descending, _) in orderingCases)
        {
            var outcome = await catalogClient.GetTourPage(1, sort, descending, ct);
            orderedPages.Add(outcome.Value);
        }

        // Assert
        var page = secondPage.Value.ShouldNotBeNull();
        page.Total.ShouldBe(tourCount);
        var identifiers = page.Items.Select(row => row.Identifier).ToArray();
        identifiers.ShouldBe([.. Enumerable.Range(10, 10).Select(index => $"SSRTOUR-{index:D3}")]);
        html.ShouldContain("<!--Blazor:", StringComparison.Ordinal);
        html.ShouldContain("SSRTOUR-000", StringComparison.Ordinal);
        html.ShouldContain("SSRTOUR-009", StringComparison.Ordinal);
        html.ShouldNotContain("SSRTOUR-010", StringComparison.Ordinal);
        PrerenderedComponentState.ShouldKeepServerStateBelow(html);
        firstViewReads.ShouldBe(2);
        var bookmarkedIdentifiers = bookmarkedHtml.Split(">SSRTOUR-", StringSplitOptions.None)
            .Skip(1)
            .Select(row => row.Split('<', 2)[0]);
        bookmarkedIdentifiers.ShouldBe(expectedBookmarkedIdentifiers);
        bookmarkedHtml.ShouldContain("<!--Blazor:", StringComparison.Ordinal);
        bookmarkedHtml.ShouldContain("aria-sort=\"descending\"", StringComparison.Ordinal);
        bookmarkedHtml.ShouldNotContain("<em>Loading...</em>", StringComparison.Ordinal);
        PrerenderedComponentState.ShouldKeepServerStateBelow(bookmarkedHtml);
        bookmarkedViewReads.ShouldBe(1);
        scenario.PageReads.ShouldBe(3 + orderingCases.Length);
        for (var index = 0; index < orderingCases.Length; index++)
        {
            var orderedPage = orderedPages[index].ShouldNotBeNull();
            orderedPage.Total.ShouldBe(tourCount);
            orderedPage.Items.Select(row => row.Identifier).ShouldBe(
                orderingCases[index].Expected.Select(row => $"SSRTOUR-{row:D3}"));
        }
    }
}
