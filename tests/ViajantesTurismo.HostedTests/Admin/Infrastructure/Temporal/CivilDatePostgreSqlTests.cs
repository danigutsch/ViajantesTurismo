using PandaBear.Testing;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Temporal;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
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
        var (BirthDate, StartDate, EndDate, BookingDate) = await scenario.Read(ct);
        // Assert
        BirthDate.ShouldBe(new DateOnly(1990, 1, 1));
        StartDate.ShouldBe(new DateOnly(2030, 2, 3));
        EndDate.ShouldBe(new DateOnly(2030, 2, 10));
        BookingDate.ShouldBe(new DateTime(2030, 2, 4, 14, 0, 0, DateTimeKind.Utc));
    }
}
