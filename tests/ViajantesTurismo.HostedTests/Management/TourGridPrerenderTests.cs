using System.Net;
using PandaBear.Testing;
using PandaBear.Testing.AspNetCore;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.HostedTests.Management.Browser;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Exercises the Tour overview through PostgreSQL, Admin HTTP and Management prerendering.</summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.Integration)]
public sealed class TourGridPrerenderTests(HostedTestFixture fixture)
{
    [Fact]
    public async Task A_large_tour_grid_only_reads_one_lightweight_page_during_prerender()
    {
        // Arrange
        const int tourCount = 125;
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.Databases.Create(ct);
        var (active, _, retired) = await TourOptionPostgreSqlScenario.Seed(database.ConnectionString, tourCount, ct);
        var newest = active.MaxBy(tour => tour.Id).ShouldNotBeNull();
        var oldest = active.MinBy(tour => tour.Id).ShouldNotBeNull();
        var probe = new TourGridQueryProbe();
        await using var api = CustomerOptionPostgreSqlScenario.Host(database.ConnectionString, probe);
        using var adminHttp = api.CreateClient();
        ApiTestAuthentication.ConfigureAuthenticatedClient(adminHttp, CustomerOptionPostgreSqlScenario.Audience, "Reader");
        await using var management = await ManagementWebBrowserHost.StartWithClients(ct, tours: new ToursApiClient(adminHttp));
        using var browser = new HttpClient { BaseAddress = management.BaseUri, Timeout = TimeSpan.FromSeconds(15) };

        // Act
        var html = await browser.GetStringAsync(new Uri("/tours", UriKind.Relative), ct);
        var renderedText = WebUtility.HtmlDecode(html);

        // Assert
        renderedText.ShouldContain($"Total tours: {tourCount}", StringComparison.Ordinal);
        renderedText.ShouldContain(newest.Name, StringComparison.Ordinal);
        renderedText.ShouldNotContain(oldest.Name, StringComparison.Ordinal);
        renderedText.ShouldNotContain(retired.Name, StringComparison.Ordinal);
        PrerenderedComponentState.ShouldKeepServerStateBelow(html);
        probe.MaterializedTours.ShouldBe(0);
        probe.TourReads.ShouldBe(2);
        probe.LimitedTourReads.ShouldBe(1);
        TestContext.Current.TestOutputHelper?.WriteLine(probe.LastLimitedProjection ?? "No limited query was recorded.");
        probe.OverviewReads.ShouldBe(1);
    }

    [Fact]
    public async Task A_tour_reader_can_request_the_second_page()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.Databases.Create(ct);
        var (active, _, _) = await TourOptionPostgreSqlScenario.Seed(database.ConnectionString, 23, ct);
        await using var api = CustomerOptionPostgreSqlScenario.Host(database.ConnectionString, new TourGridQueryProbe());
        using var adminHttp = api.CreateClient();
        ApiTestAuthentication.ConfigureAuthenticatedClient(adminHttp, CustomerOptionPostgreSqlScenario.Audience, "Reader");

        // Act
        using var response = await adminHttp.GetAsync(new Uri("/api/v1/tours/page?page=2", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GetTourPageDto>(ct);
        var page = payload.ShouldNotBeNull();
        page.Total.ShouldBe(active.Count);
        page.Items.Select(item => item.Id).ShouldBe(active.OrderByDescending(tour => tour.Id)
            .Skip(ContractConstants.TourListPageSize).Take(ContractConstants.TourListPageSize).Select(tour => tour.Id));
    }

    [Fact]
    public async Task Sorting_tours_uses_stable_database_windows_and_preserves_capacity_facts()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.Databases.Create(ct);
        var (tours, full) = await TourGridSortPostgreSqlScenario.Seed(database.ConnectionString, ct);
        var probe = new TourGridQueryProbe();
        await using var api = CustomerOptionPostgreSqlScenario.Host(database.ConnectionString, probe);
        using var http = api.CreateClient();
        ApiTestAuthentication.ConfigureAuthenticatedClient(http, CustomerOptionPostgreSqlScenario.Audience, "Operator");
        var client = new ToursApiClient(http);
        (TourPageSort Sort, bool Descending, IOrderedEnumerable<Tour> Order)[] sorts =
        [
            (TourPageSort.Identifier, false, tours.OrderBy(tour => tour.Identifier, StringComparer.Ordinal)),
            (TourPageSort.Identifier, true, tours.OrderByDescending(tour => tour.Identifier, StringComparer.Ordinal)),
            (TourPageSort.Name, false, tours.OrderBy(tour => tour.Name, StringComparer.Ordinal)),
            (TourPageSort.Name, true, tours.OrderByDescending(tour => tour.Name, StringComparer.Ordinal)),
            (TourPageSort.StartDate, false, tours.OrderBy(tour => tour.Schedule.StartDate)),
            (TourPageSort.StartDate, true, tours.OrderByDescending(tour => tour.Schedule.StartDate)),
            (TourPageSort.EndDate, false, tours.OrderBy(tour => tour.Schedule.EndDate)),
            (TourPageSort.EndDate, true, tours.OrderByDescending(tour => tour.Schedule.EndDate)),
            (TourPageSort.Price, false, tours.OrderBy(tour => tour.Pricing.BasePrice)),
            (TourPageSort.Price, true, tours.OrderByDescending(tour => tour.Pricing.BasePrice)),
            (TourPageSort.CurrentCustomerCount, false, tours.OrderBy(tour => tour.CurrentCustomerCount)),
            (TourPageSort.CurrentCustomerCount, true, tours.OrderByDescending(tour => tour.CurrentCustomerCount))
        ];

        // Act
        var pages = new List<GetTourPageDto?>(sorts.Length);
        foreach (var (sort, descending, _) in sorts)
        {
            var result = await client.GetTourPage(2, sort, descending, ct);
            pages.Add(result.Value);
        }

        var capacityPage = await client.GetTourPage(1, TourPageSort.CurrentCustomerCount, descending: true, ct);
        var lastPage = await client.GetTourPage(int.MaxValue, TourPageSort.Default, descending: false, ct);
        var firstPage = await client.GetTourPage(0, TourPageSort.Default, descending: false, ct);

        // Assert
        for (var index = 0; index < sorts.Length; index++)
        {
            var page = pages[index].ShouldNotBeNull();
            var expected = sorts[index].Order.ThenByDescending(tour => tour.Id)
                .Skip(ContractConstants.TourListPageSize).Take(ContractConstants.TourListPageSize).Select(tour => tour.Id);
            page.Total.ShouldBe(tours.Count);
            page.Items.Select(item => item.Id).ShouldBe(expected);
        }

        var fullRow = capacityPage.Value.ShouldNotBeNull().Items[0];
        fullRow.Id.ShouldBe(full.Id);
        fullRow.CurrentCustomerCount.ShouldBe(3);
        fullRow.AvailableSpots.ShouldBe(full.AvailableSpots);
        fullRow.IsFullyBooked.ShouldBe(full.IsFullyBooked);
        fullRow.IsAtMinimumCapacity.ShouldBe(full.IsAtMinimumCapacity);
        fullRow.Price.ShouldBe(full.Pricing.BasePrice);
        fullRow.Currency.ShouldBe(full.Pricing.Currency.Code);
        firstPage.Value.ShouldNotBeNull().Items.Select(item => item.Id)
            .ShouldBe(tours.OrderByDescending(tour => tour.Id).Take(ContractConstants.TourListPageSize).Select(tour => tour.Id));
        lastPage.Value.ShouldNotBeNull().Items.Select(item => item.Id)
            .ShouldBe(tours.OrderByDescending(tour => tour.Id).Skip(2 * ContractConstants.TourListPageSize).Select(tour => tour.Id));
        var pageReads = sorts.Length + 3;
        probe.TourReads.ShouldBe(2 * pageReads);
        probe.LimitedTourReads.ShouldBe(pageReads);
        TestContext.Current.TestOutputHelper?.WriteLine(probe.LastLimitedProjection ?? "No limited query was recorded.");
        probe.OverviewReads.ShouldBe(pageReads);
        probe.MaterializedTours.ShouldBe(0);
    }
}
