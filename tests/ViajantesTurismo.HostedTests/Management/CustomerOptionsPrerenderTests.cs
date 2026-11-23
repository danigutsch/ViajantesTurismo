using Microsoft.Extensions.Logging.Abstractions;
using PandaBear.Testing;
using PandaBear.Testing.AspNetCore;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;
using ViajantesTurismo.Admin.Testing.Builders;
using ViajantesTurismo.Admin.Testing.Fakes.ApiClients;
using ViajantesTurismo.HostedTests.Management.Browser;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Verifies the customer companion picker against a large migrated PostgreSQL database.</summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.Integration)]
public sealed class CustomerOptionsPrerenderTests(HostedTestFixture fixture)
{
    [Fact]
    public async Task Large_customer_dropdown_reads_a_bounded_slice_only_after_prerender()
    {
        // Arrange
        const int customerCount = 800;
        const int matchingCount = 40;
        const int pageLimit = 12;
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.Databases.Create(ct);
        var matching = await CustomerOptionPostgreSqlScenario.Seed(database.ConnectionString, customerCount, matchingCount, ct);
        var query = new CustomerOptionQueryProbe();
        await using var adminApi = CustomerOptionPostgreSqlScenario.Host(database.ConnectionString, query);
        using var adminHttp = adminApi.CreateClient();
        ApiTestAuthentication.ConfigureAuthenticatedClient(adminHttp, CustomerOptionPostgreSqlScenario.Audience, "Reader");
        var client = new CustomersApiClient(adminHttp, NullLogger<CustomersApiClient>.Instance);
        var booking = DtoBuilders.BuildBookingDto(customerId: matching[0], companionId: matching[^1], companionName: "Retained Companion");
        var bookings = new FakeBookingsApiClient();
        bookings.AddBooking(booking);
        var tour = DtoBuilders.BuildTourDto();
        var tours = new FakeToursApiClient();
        tours.AddTour(tour);
        await using var management = await ManagementWebBrowserHost.StartWithClients(
            ct, bookings: bookings, customers: client, tours: tours, countries: new FakeCountryBrowserService(),
            clock: TimeProvider.System);
        using var browser = new HttpClient { BaseAddress = management.BaseUri, Timeout = TimeSpan.FromSeconds(15) };

        // Act
        var html = await browser.GetStringAsync(new Uri("/customers/create/accommodation", UriKind.Relative), ct);
        var editHtml = await browser.GetStringAsync(new Uri($"/bookings/{booking.Id}/edit", UriKind.Relative), ct);
        var readsDuringPrerender = query.Reads;
        var limitedBeforeDetails = query.LimitedReads;
        var tourHtml = await browser.GetStringAsync(new Uri($"/tours/{tour.Id}", UriKind.Relative), ct);
        var customerHtml = await browser.GetStringAsync(new Uri($"/customers/{matching[0]}", UriKind.Relative), ct);
        var customerEditHtml = await browser.GetStringAsync(new Uri($"/customers/{matching[0]}/edit", UriKind.Relative), ct);
        var readsBeforeSearch = query.Reads;
        var limitedBeforeSearch = query.LimitedReads;
        var first = await client.SearchCustomerOptions(new CustomerOptionSearchDto(null, 800), ct);
        var folded = await client.SearchCustomerOptions(new CustomerOptionSearchDto("  JOSE   ÁVILA  ", pageLimit), ct);
        var accents = await client.SearchCustomerOptions(new CustomerOptionSearchDto("jose\u0301 avila", pageLimit), ct);
        var literal = await client.SearchCustomerOptions(new CustomerOptionSearchDto("jo_e", pageLimit), ct);
        var selected = await client.SearchCustomerOptions(new CustomerOptionSearchDto(matching[^1].ToString("D"), 1), ct);
        var eligible = await client.SearchCustomerOptions(new CustomerOptionSearchDto("jose avila", pageLimit, matching[0]), ct);
        var detailReads = readsBeforeSearch - readsDuringPrerender;
        var limitedDetailReads = limitedBeforeSearch - limitedBeforeDetails;
        var searchReads = query.Reads - readsBeforeSearch;
        var limitedSearchReads = query.LimitedReads - limitedBeforeSearch;

        // Assert
        readsDuringPrerender.ShouldBe(0);
        html.ShouldContain("<!--Blazor:", StringComparison.Ordinal);
        html.ShouldContain("id=\"companionSearch\"", StringComparison.Ordinal);
        html.ShouldNotContain("choice-799@example.test", StringComparison.Ordinal);
        PrerenderedComponentState.ShouldKeepServerStateBelow(html);
        editHtml.ShouldContain("<!--Blazor:", StringComparison.Ordinal);
        editHtml.ShouldContain("id=\"companionSearch\"", StringComparison.Ordinal);
        editHtml.ShouldContain("Retained Companion", StringComparison.Ordinal);
        PrerenderedComponentState.ShouldKeepServerStateBelow(editHtml);
        detailReads.ShouldBe(2);
        limitedDetailReads.ShouldBe(2);
        tours.GetToursCallCount.ShouldBe(0);
        tourHtml.ShouldContain("Tour Details", StringComparison.Ordinal);
        customerHtml.ShouldContain("Customer Details", StringComparison.Ordinal);
        tourHtml.ShouldNotContain("id=\"customerSearch\"", StringComparison.Ordinal);
        customerHtml.ShouldNotContain("id=\"tourSearch\"", StringComparison.Ordinal);
        PrerenderedComponentState.ShouldKeepServerStateBelow(tourHtml);
        PrerenderedComponentState.ShouldKeepServerStateBelow(customerHtml);
        customerEditHtml.ShouldContain("id=\"companionSearch\"", StringComparison.Ordinal);
        customerEditHtml.ShouldContain("Edit Customer", StringComparison.Ordinal);
        PrerenderedComponentState.ShouldKeepServerStateBelow(customerEditHtml);
        var firstPage = first.Value.ShouldNotBeNull();
        firstPage.Items.Count.ShouldBe(ContractConstants.MaxCustomerOptions);
        firstPage.Total.ShouldBe(customerCount);
        var choices = folded.Value.ShouldNotBeNull();
        choices.Total.ShouldBe(matchingCount);
        choices.Items.Select(item => item.Id).ShouldBe(matching.Take(pageLimit));
        accents.Value.ShouldNotBeNull().Items.ShouldBe(choices.Items);
        literal.Value.ShouldNotBeNull().Items.ShouldBeEmpty();
        selected.Value.ShouldNotBeNull().Items.ShouldHaveSingleItem().Id.ShouldBe(matching[^1]);
        var eligibleChoices = eligible.Value.ShouldNotBeNull();
        eligibleChoices.Total.ShouldBe(matchingCount - 1);
        eligibleChoices.Items.Select(item => item.Id).ShouldBe(matching.Skip(1).Take(pageLimit));
        eligibleChoices.Items.Select(item => item.BikeType).ShouldBe(Enumerable.Repeat(BikeTypeDto.EBike, pageLimit));
        searchReads.ShouldBe(12);
        limitedSearchReads.ShouldBe(6);
    }
}
