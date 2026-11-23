using Microsoft.Extensions.DependencyInjection;
using PandaBear.Testing;
using PandaBear.Testing.AspNetCore;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.HostedTests.Management.Browser;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Exercises the bounded booking list through PostgreSQL, HTTP and Management prerendering.</summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.Integration)]
public sealed class BookingListPrerenderTests(HostedTestFixture fixture)
{
    [Fact]
    public async Task Large_booking_overview_reads_only_the_requested_summaries()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.Databases.Create(ct);
        var bookings = await BookingPagePostgreSqlScenario.Seed(database.ConnectionString, ct);
        var total = BookingPagePostgreSqlScenario.TourCount * BookingPagePostgreSqlScenario.BookingsPerTour;
        var size = ContractConstants.BookingListPageSize;
        var expected = bookings.OrderByDescending(booking => booking.Status == BookingStatus.Cancelled)
            .ThenByDescending(booking => booking.BookingDate)
            .ThenBy(booking => booking.Id)
            .ToArray();
        var materialization = new BookingPageMaterializationProbe();
        await using var adminApi = BookingPagePostgreSqlScenario.Host(database.ConnectionString, materialization);
        using var adminHttp = adminApi.CreateClient();
        ApiTestAuthentication.ConfigureAuthenticatedClient(adminHttp, BookingPagePostgreSqlScenario.Audience, "Reader");
        var client = new BookingsApiClient(adminHttp);
        var registrations = new ServiceCollection();
        registrations.AddSingleton<IBookingsApiClient>(client);
        await using var management = await ManagementWebBrowserHost.Start(registrations, ct);
        using var browser = new HttpClient { BaseAddress = management.BaseUri, Timeout = TimeSpan.FromSeconds(15) };

        // Act
        var secondPage = await client.GetBookingPage(2, BookingPageSort.Default, descending: false, ct);
        var lowPrices = await client.GetBookingPage(1, BookingPageSort.TotalPrice, descending: false, ct);
        var highPrices = await client.GetBookingPage(1, BookingPageSort.TotalPrice, descending: true, ct);
        var lastPage = await client.GetBookingPage(int.MaxValue, BookingPageSort.BookingDate, descending: false, ct);
        var byTour = await client.GetBookingPage(1, BookingPageSort.TourName, descending: false, ct);
        var byCustomer = await client.GetBookingPage(1, BookingPageSort.CustomerName, descending: true, ct);
        var html = await browser.GetStringAsync(new Uri("/bookings", UriKind.Relative), ct);

        // Assert
        var page = secondPage.Value.ShouldNotBeNull();
        page.Counts.ShouldBe(new BookingOverviewCountsDto(total, total / 4, total / 4, total / 4, total / 4));
        page.Items.Select(row => row.Id).ShouldBe(expected.Skip(size).Take(size).Select(booking => booking.Id));
        lowPrices.Value.ShouldNotBeNull().Items.Select(row => (row.Id, row.TotalPrice)).ShouldBe(
            bookings.OrderBy(booking => booking.TotalPrice).ThenBy(booking => booking.Id).Take(size)
                .Select(booking => (booking.Id, booking.TotalPrice)));
        highPrices.Value.ShouldNotBeNull().Items.Select(row => (row.Id, row.TotalPrice)).ShouldBe(
            bookings.OrderByDescending(booking => booking.TotalPrice).ThenBy(booking => booking.Id).Take(size)
                .Select(booking => (booking.Id, booking.TotalPrice)));
        lastPage.Value.ShouldNotBeNull().Items.Select(row => row.Id).ShouldBe(
            bookings.OrderBy(booking => booking.BookingDate).ThenBy(booking => booking.Id).TakeLast(size)
                .Select(booking => booking.Id));
        byTour.Value.ShouldNotBeNull().Items.Select(row => row.TourName).ShouldBe(Enumerable.Repeat("Paging Tour 00", size));
        var customerItems = byCustomer.Value.ShouldNotBeNull().Items;
        customerItems.Count.ShouldBe(size);
        customerItems.All(row => row.CustomerName.StartsWith("PagingCustomer15 ", StringComparison.Ordinal)).ShouldBeTrue();
        html.ShouldContain("<!--Blazor:", StringComparison.Ordinal);
        html.ShouldContain($"Total: {total}", StringComparison.Ordinal);
        html.ShouldContain($"/bookings/{expected[0].Id}", StringComparison.Ordinal);
        html.ShouldContain($"/bookings/{expected[size - 1].Id}", StringComparison.Ordinal);
        html.ShouldNotContain($"/bookings/{expected[size].Id}", StringComparison.Ordinal);
        PrerenderedComponentState.ShouldKeepServerStateBelow(html);
        materialization.Bookings.ShouldBe(7 * size);
    }
}
