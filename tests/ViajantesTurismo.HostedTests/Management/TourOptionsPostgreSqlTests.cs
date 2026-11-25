using SharedKernel.HttpClients;
using SharedKernel.Testing;
using SharedKernel.Testing.AspNetCore;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Exercises the complete tour-option read path against PostgreSQL.</summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.Integration)]
public sealed class TourOptionsPostgreSqlTests(HostedTestFixture fixture)
{
    [Fact]
    public async Task Tour_options_filter_active_choices_and_bound_the_SQL_projection()
    {
        // Arrange
        const int tourCount = 125;
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.Databases.Create(ct);
        var (active, full, retired) = await TourOptionPostgreSqlScenario.Seed(database.ConnectionString, tourCount, ct);
        var probe = new TourOptionQueryProbe();
        await using var api = CustomerOptionPostgreSqlScenario.Host(database.ConnectionString, probe);
        using var http = api.CreateClient();
        ApiTestAuthentication.ConfigureAuthenticatedClient(http, CustomerOptionPostgreSqlScenario.Audience, "Reader");
        var client = new ToursApiClient(http);

        // Act
        var firstPage = await client.SearchTourOptions(new TourOptionSearchDto(null, 500), ct);
        var byName = await client.SearchTourOptions(new TourOptionSearchDto("  SÉRRA   AGUAS  ", 10), ct);
        var byIdentifier = await client.SearchTourOptions(new TourOptionSearchDto("aguas-only", 1), ct);
        var wildcard = await client.SearchTourOptions(new TourOptionSearchDto("OPTION-%", 10), ct);
        var optionReads = probe.Reads;
        var limitedOptionReads = probe.LimitedReads;
        var optionProjectionReads = probe.ChoiceProjectionReads;
        var optionBookingReads = probe.BookingReads;
        var selected = await client.GetTourById(full.Id, ct);
        var retiredDetail = await client.GetTourById(retired.Id, ct);

        // Assert
        var first = firstPage.Value.ShouldNotBeNull();
        first.Total.ShouldBe(tourCount);
        first.Items.Count.ShouldBe(ContractConstants.MaxTourOptions);
        first.Items.Select(item => item.Id).ShouldBe(active.Select(tour => tour.Id).OrderDescending().Take(ContractConstants.MaxTourOptions));
        var expected = new GetTourOptionDto(full.Id, full.Name, full.Identifier, new DateOnly(2029, 5, 3));
        byName.Value.ShouldNotBeNull().Items.ShouldHaveSingleItem().ShouldBe(expected);
        byName.Value.ShouldNotBeNull().Total.ShouldBe(1);
        byIdentifier.Value.ShouldNotBeNull().Items.ShouldHaveSingleItem().ShouldBe(expected);
        wildcard.Value.ShouldNotBeNull().Items.ShouldBeEmpty();
        optionReads.ShouldBe(8);
        limitedOptionReads.ShouldBe(4);
        optionProjectionReads.ShouldBe(4);
        optionBookingReads.ShouldBe(0);
        var details = selected.Value.ShouldNotBeNull();
        details.IsFullyBooked.ShouldBeTrue();
        details.Price.ShouldBe(432.10m);
        retiredDetail.Kind.ShouldBe(HttpOutcomeKind.NotFound);
    }
}
