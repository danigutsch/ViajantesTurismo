using SharedKernel.Testing;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Tours;

[Trait(TestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class BookingCommitAmbiguityPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Retrying_after_the_booking_commit_returns_the_original_booking_without_duplicate_rows()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);

        // Act
        var (result, attempts, initiallyCommitted, persisted) =
            await BookingCommitAmbiguityPostgreSqlScenario.CreateBooking(database.ConnectionString, ct);

        // Assert
        var originalBookingId = initiallyCommitted.ShouldHaveSingleItem();
        attempts.ShouldBe(2);
        result.IsSuccess.ShouldBeTrue(result.ErrorDetails?.Detail);
        persisted.ShouldBe([originalBookingId]);
        result.Value.ShouldBe(originalBookingId);
    }
}
