using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using PandaBear.Testing;
using PandaBear.Testing.AspNetCore;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.HostedTests.Management.Browser;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Exercises owner-filtered histories through PostgreSQL, Admin HTTP and Management prerendering.</summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.Integration)]
public sealed class BookingHistoryPrerenderTests(HostedTestFixture fixture)
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task The_owner_details_page_only_materializes_the_first_booking_window(bool byTour)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.Databases.Create(ct);
        var bookings = await BookingPagePostgreSqlScenario.Seed(database.ConnectionString, ct);
        var booking = bookings[0];
        var customerId = booking.CompanionCustomer.ShouldNotBeNull().CustomerId;
        var ordered = byTour
            ? bookings.Where(item => item.TourId == booking.TourId)
                .OrderByDescending(item => item.Status == BookingStatus.Cancelled)
                .ThenByDescending(item => item.BookingDate)
            : bookings.Where(item => item.CompanionCustomer?.CustomerId == customerId)
                .OrderByDescending(item => item.BookingDate);
        var expected = ordered.ThenBy(item => item.Id).ToArray();
        var probe = new BookingPageMaterializationProbe();
        await using var api = BookingPagePostgreSqlScenario.Host(database.ConnectionString, probe);
        using var adminHttp = api.CreateClient();
        ApiTestAuthentication.ConfigureAuthenticatedClient(adminHttp, BookingPagePostgreSqlScenario.Audience, "Reader");
        await using var management = await ManagementWebBrowserHost.StartWithClients(
            ct, bookings: new BookingsApiClient(adminHttp),
            customers: new CustomersApiClient(adminHttp, NullLogger<CustomersApiClient>.Instance),
            tours: new ToursApiClient(adminHttp));
        using var browser = new HttpClient { BaseAddress = management.BaseUri, Timeout = TimeSpan.FromSeconds(15) };
        var route = byTour ? $"/tours/{booking.TourId}" : $"/customers/{customerId}";
        var size = ContractConstants.BookingListPageSize;

        // Act
        var html = await browser.GetStringAsync(new Uri(route, UriKind.Relative), ct);

        // Assert
        TestContext.Current.TestOutputHelper?.WriteLine($"Materialized {probe.Bookings} booking rows from {expected.Length} matching rows.");
        probe.Bookings.ShouldBe(size);
        html.ShouldContain($"/bookings/{expected[0].Id}", StringComparison.Ordinal);
        html.ShouldContain($"/bookings/{expected[size - 1].Id}", StringComparison.Ordinal);
        html.ShouldNotContain($"/bookings/{expected[size].Id}", StringComparison.Ordinal);
        PrerenderedComponentState.ShouldKeepServerStateBelow(html);
    }

    [Theory]
    [InlineData("tour")]
    [InlineData("customer")]
    public async Task A_reader_can_request_the_second_owner_history_page(string owner)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.Databases.Create(ct);
        var bookings = await BookingPagePostgreSqlScenario.Seed(database.ConnectionString, ct);
        var booking = bookings[0];
        var ownerId = owner == "tour" ? booking.TourId : booking.PrincipalCustomer.CustomerId;
        var ordered = owner == "tour"
            ? bookings.Where(item => item.TourId == ownerId).OrderByDescending(item => item.Status == BookingStatus.Cancelled)
                .ThenByDescending(item => item.BookingDate)
            : bookings.Where(item => item.PrincipalCustomer.CustomerId == ownerId || item.CompanionCustomer?.CustomerId == ownerId)
                .OrderByDescending(item => item.BookingDate);
        var expected = ordered.ThenBy(item => item.Id).ToArray();
        var probe = new BookingPageMaterializationProbe();
        await using var api = BookingPagePostgreSqlScenario.Host(database.ConnectionString, probe);
        using var adminHttp = api.CreateClient();
        ApiTestAuthentication.ConfigureAuthenticatedClient(adminHttp, BookingPagePostgreSqlScenario.Audience, "Reader");
        var client = new BookingsApiClient(adminHttp);
        var size = ContractConstants.BookingListPageSize;

        // Act
        using var response = await adminHttp.GetAsync(new Uri($"/api/v1/bookings/{owner}/{ownerId}/page?page=2", UriKind.Relative), ct);
        var page = await response.Content.ReadFromJsonAsync<GetBookingHistoryPageDto>(ct);
        var last = owner == "tour"
            ? await client.GetTourBookingPage(ownerId, int.MaxValue, BookingPageSort.TotalPrice, descending: true, ct)
            : await client.GetCustomerBookingPage(ownerId, int.MaxValue, BookingPageSort.TotalPrice, descending: true, ct);
        var empty = owner == "tour"
            ? await client.GetTourBookingPage(Guid.Empty, 1, BookingPageSort.Default, descending: false, ct)
            : await client.GetCustomerBookingPage(Guid.Empty, 1, BookingPageSort.Default, descending: false, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        page.ShouldNotBeNull();
        page.Total.ShouldBe(expected.Length);
        page.Items.Select(item => item.Id).ShouldBe(expected.Skip(size).Take(size).Select(item => item.Id));
        var lastSize = ((expected.Length - 1) % size) + 1;
        last.Value.ShouldNotBeNull().Items.Select(item => item.Id).ShouldBe(expected.OrderByDescending(item => item.TotalPrice)
            .ThenBy(item => item.Id).TakeLast(lastSize).Select(item => item.Id));
        empty.Value.ShouldNotBeNull().Total.ShouldBe(0);
        empty.Value.ShouldNotBeNull().Items.ShouldBeEmpty();
        probe.Bookings.ShouldBe(Math.Min(size, expected.Length - size) + lastSize);
    }

    [Fact]
    public async Task A_tour_details_header_reads_confirmed_capacity_without_loading_bookings()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.Databases.Create(ct);
        var (_, full) = await TourGridSortPostgreSqlScenario.Seed(database.ConnectionString, ct);
        var probe = new BookingPageMaterializationProbe();
        await using var api = BookingPagePostgreSqlScenario.Host(database.ConnectionString, probe);
        using var adminHttp = api.CreateClient();
        ApiTestAuthentication.ConfigureAuthenticatedClient(adminHttp, BookingPagePostgreSqlScenario.Audience, "Reader");
        var client = new ToursApiClient(adminHttp);

        // Act
        var outcome = await client.GetTourById(full.Id, ct);

        // Assert
        var details = outcome.Value.ShouldNotBeNull();
        details.CurrentCustomerCount.ShouldBe(3);
        details.AvailableSpots.ShouldBe(full.AvailableSpots);
        details.IsAtMinimumCapacity.ShouldBe(full.IsAtMinimumCapacity);
        details.IsFullyBooked.ShouldBe(full.IsFullyBooked);
        details.IncludedServices.ShouldBe([.. full.IncludedServices]);
        probe.Bookings.ShouldBe(0);
    }
}
