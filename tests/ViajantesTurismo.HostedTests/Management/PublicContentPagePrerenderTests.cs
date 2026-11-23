using System.Net;
using PandaBear.Testing;
using PandaBear.Testing.AspNetCore;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.Http;
using ViajantesTurismo.Catalog.Testing.Infrastructure;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Exercises the Catalog content editor through PostgreSQL, its authenticated API and Management prerendering.</summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.Integration)]
public sealed class PublicContentPagePrerenderTests(HostedTestFixture fixture)
{
    [Fact]
    public async Task A_large_content_library_prerenders_only_a_page_of_list_facts()
    {
        // Arrange
        const int count = 800;
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.Databases.Create(ct);
        await PublicContentPagePostgreSqlScenario.Seed(database.ConnectionString, count, addPageKey: false, ct);
        var probe = new PublicContentPageQueryProbe();
        var (client, host) = CatalogApiTestHost.CreateProductionClient(builder =>
            PublicContentPagePostgreSqlScenario.Configure(builder, database.ConnectionString, probe), "Operator");
        await using var api = host;
        using var catalogHttp = client;
        await using var management = await PublicContentPagePostgreSqlScenario.StartManagement(catalogHttp, ct);
        using var browser = new HttpClient { BaseAddress = management.BaseUri, Timeout = TimeSpan.FromSeconds(20) };

        // Act
        var html = await browser.GetStringAsync(new Uri("/catalog/content", UriKind.Relative), ct);

        // Assert
        TestContext.Current.TestOutputHelper?.WriteLine($"Materialized {probe.Entries} entries and {probe.Variants} variants; markup included final row: {html.Contains("CATALOG.ENTRY.0799", StringComparison.Ordinal)}.");
        probe.Entries.ShouldBe(0);
        probe.Variants.ShouldBe(0);
        probe.Reads.ShouldBe(2);
        var pageQuery = probe.LastLimitedRead.ShouldNotBeNull();
        pageQuery.ShouldContain("\"Key\"", StringComparison.Ordinal);
        pageQuery.ShouldContain("\"PublicationState\"", StringComparison.Ordinal);
        pageQuery.ShouldNotContain("\"PublicContentVariants\"", StringComparison.Ordinal);
        html.ShouldContain("CATALOG.ENTRY.0000", StringComparison.Ordinal);
        html.ShouldContain("CATALOG.ENTRY.0019", StringComparison.Ordinal);
        html.ShouldNotContain("CATALOG.ENTRY.0020", StringComparison.Ordinal);
        html.ShouldNotContain("Publication body", StringComparison.Ordinal);
        html.ShouldContain("/catalog/content?page=2", StringComparison.Ordinal);
        html.ShouldContain("Content pages", StringComparison.Ordinal);
        PrerenderedComponentState.ShouldKeepServerStateBelow(html);
    }

    [Fact]
    public async Task An_operator_can_request_the_second_content_overview_page()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.Databases.Create(ct);
        await PublicContentPagePostgreSqlScenario.Seed(database.ConnectionString, 25, addPageKey: true, ct);
        var probe = new PublicContentPageQueryProbe();
        var (client, host) = CatalogApiTestHost.CreateProductionClient(builder =>
            PublicContentPagePostgreSqlScenario.Configure(builder, database.ConnectionString, probe), "Operator");
        await using var api = host;
        using var catalogHttp = client;
        var contentClient = new PublicContentApiClient(catalogHttp);
        await using var management = await PublicContentPagePostgreSqlScenario.StartManagement(catalogHttp, ct);
        using var browser = new HttpClient { BaseAddress = management.BaseUri, Timeout = TimeSpan.FromSeconds(20) };

        // Act
        using var response = await catalogHttp.GetAsync(new Uri("/api/v1/catalog/public-content-overview?page=2", UriKind.Relative), ct);
        var page = await response.Content.ReadFromJsonAsync<PublicContentOverviewPageDto>(ct);
        var clamped = await contentClient.GetContentPage("999999999999999999", ct);
        var existing = await contentClient.GetContent("page", ct);
        var html = await browser.GetStringAsync(new Uri("/catalog/content?page=2", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        page.ShouldNotBeNull();
        page.Total.ShouldBe(26);
        page.Items.Select(item => item.Key).ShouldBe(
            Enumerable.Range(20, 5).Select(index => $"CATALOG.ENTRY.{index:D4}").Append("PAGE"));
        page.Items[0].PublicationState.ShouldBe("Published");
        page.Items[1].PublicationState.ShouldBe("ReviewRequired");
        var lastPage = clamped.Value.ShouldNotBeNull();
        lastPage.Total.ShouldBe(page.Total);
        lastPage.Items.ShouldBe([.. page.Items]);
        var existingPage = existing.Value.ShouldNotBeNull();
        existingPage.Key.ShouldBe("PAGE");
        existingPage.Variants.Count.ShouldBe(2);
        existingPage.Variants.ShouldHaveSingleItem(variant => variant.Language == "en-US").Title.ShouldBe("Existing Page");
        html.ShouldContain("CATALOG.ENTRY.0020", StringComparison.Ordinal);
        html.ShouldNotContain("CATALOG.ENTRY.0019", StringComparison.Ordinal);
        html.ShouldContain("aria-current=\"page\"", StringComparison.Ordinal);
        PrerenderedComponentState.ShouldKeepServerStateBelow(html);
    }
}
