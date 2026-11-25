using SharedKernel.BuildingBlocks;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Tours;

/// <summary>Proves tours store the ISO 4217 code in a three-character column.</summary>
[Trait(TestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class TourCurrencyPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
{
    [Theory]
    [InlineData("BRL")]
    [InlineData("EUR")]
    [InlineData("USD")]
    public async Task A_tour_stores_its_currency_as_the_iso_code_and_reads_it_back(string code)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var currency = Currency.Create(code).Value;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        await using var session = scenario.Open();
        await session.Migrate(ct);
        var tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(Currency: currency)));
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;

        // Act
        await session.Seed(tour, ct);
        var stored = await TourCurrencyDatabase.ReadCurrency(scenario, tour.Identifier, ct);
        var maxLength = await TourCurrencyDatabase.ReadCurrencyColumnMaxLength(scenario, ct);
        await using var restarted = scenario.Open();
        var reloaded = await restarted.ReloadTour(booking.Id, ct);

        // Assert
        stored.ShouldBe(code);
        maxLength.ShouldBe(3);
        reloaded.ShouldNotBeNull().Pricing.Currency.ShouldBe(currency);
    }
}
