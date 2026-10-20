using SharedKernel.Testing;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Temporal;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
public sealed class CivilDatePostgreSqlTests(PostgreSqlTestServerFixture fixture)
{
    [Fact]
    public async Task Civil_dates_and_captured_booking_and_payment_instants_round_trip()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await CivilDatePostgreSqlScenario.Create(fixture, ct);
        await scenario.Seed(ct);
        // Act
        var actual = await scenario.Read(ct);
        // Assert
        actual.BirthDate.ShouldBe(new DateOnly(1990, 1, 1));
        actual.StartDate.ShouldBe(new DateOnly(2030, 2, 3));
        actual.EndDate.ShouldBe(new DateOnly(2030, 2, 10));
        actual.PaymentDate.ShouldBe(new DateOnly(2030, 2, 3));
        actual.BookingDate.ShouldBe(new DateTime(2030, 2, 4, 14, 0, 0, DateTimeKind.Utc));
        actual.RecordedAt.ShouldBe(actual.BookingDate);
        actual.RecordedAt.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData("Pacific/Kiritimati")]
    [InlineData("America/Los_Angeles")]
    public async Task Migration_and_rollback_preserve_utc_calendar_days_in_non_utc_sessions(string timeZone)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await CivilDatePostgreSqlScenario.Create(fixture, ct);
        await scenario.Seed(ct);
        await scenario.Migrate(forward: false, timeZone, ct);
        await scenario.SetLegacyDates(ct);
        // Act
        await scenario.Migrate(forward: true, timeZone, ct);
        var first = await scenario.Read(ct);
        await scenario.Migrate(forward: false, timeZone, ct);
        await scenario.Migrate(forward: true, timeZone, ct);
        var recovered = await scenario.Read(ct);
        // Assert
        first.BirthDate.ShouldBe(new DateOnly(1990, 1, 1));
        first.StartDate.ShouldBe(new DateOnly(2030, 2, 3));
        first.EndDate.ShouldBe(new DateOnly(2030, 2, 10));
        first.PaymentDate.ShouldBe(new DateOnly(2030, 2, 3));
        recovered.ShouldBe(first);
    }
}
