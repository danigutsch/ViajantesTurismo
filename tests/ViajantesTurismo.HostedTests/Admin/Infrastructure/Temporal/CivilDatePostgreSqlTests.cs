using PandaBear.Testing;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Temporal;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
public sealed class CivilDatePostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Civil_dates_and_captured_booking_instants_round_trip()
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
        actual.BookingDate.ShouldBe(new DateTime(2030, 2, 4, 14, 0, 0, DateTimeKind.Utc));
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
        await scenario.SetTimestampDates(ct);
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
        recovered.ShouldBe(first);
    }
}
