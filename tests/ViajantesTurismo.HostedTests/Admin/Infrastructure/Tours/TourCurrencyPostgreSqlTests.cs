using PandaBear.BuildingBlocks;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Tours;

/// <summary>Proves tours store the ISO 4217 code and that the migration converts rows written with the former enum names.</summary>
[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class TourCurrencyPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
{
    private const string MigrationBeforeIsoCurrency = "20261101154756_DocumentManualReview";

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
        await using var restarted = scenario.Open();
        var reloaded = await restarted.ReloadTour(booking.Id, ct);

        // Assert
        stored.ShouldBe(code);
        reloaded.ShouldNotBeNull().Pricing.Currency.ShouldBe(currency);
    }

    [Fact]
    public async Task The_migration_converts_legacy_currency_names_to_iso_codes_and_back()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        await using var session = scenario.Open();
        await session.Migrate(ct, MigrationBeforeIsoCurrency);
        await TourCurrencyDatabase.InsertTour(scenario, "legacy-real", "Real", ct);
        await TourCurrencyDatabase.InsertTour(scenario, "legacy-euro", "Euro", ct);
        await TourCurrencyDatabase.InsertTour(scenario, "legacy-dollar", "UsDollar", ct);

        // Act
        await session.Migrate(ct);
        var upgraded = new[]
        {
            await TourCurrencyDatabase.ReadCurrency(scenario, "legacy-real", ct),
            await TourCurrencyDatabase.ReadCurrency(scenario, "legacy-euro", ct),
            await TourCurrencyDatabase.ReadCurrency(scenario, "legacy-dollar", ct)
        };
        var maxLength = await TourCurrencyDatabase.ReadCurrencyColumnMaxLength(scenario, ct);
        await session.Migrate(ct, MigrationBeforeIsoCurrency);
        var downgraded = new[]
        {
            await TourCurrencyDatabase.ReadCurrency(scenario, "legacy-real", ct),
            await TourCurrencyDatabase.ReadCurrency(scenario, "legacy-euro", ct),
            await TourCurrencyDatabase.ReadCurrency(scenario, "legacy-dollar", ct)
        };

        // Assert
        upgraded.ShouldBe(["BRL", "EUR", "USD"]);
        maxLength.ShouldBe(3);
        downgraded.ShouldBe(["Real", "Euro", "UsDollar"]);
    }
}
