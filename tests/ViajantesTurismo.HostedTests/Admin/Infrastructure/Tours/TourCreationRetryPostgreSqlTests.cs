using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Tours.CreateTour;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Tours;

[Trait(TestTraitNames.CategoryName, SharedKernel.IntegrationTesting.IntegrationTestTraitValues.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class TourCreationRetryPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Serialization_failure_replays_tour_creation_and_commits_one_outbox_event()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);
        var command = new CreateTourCommand(
            "RETRYTOUR", "Retry tour", new DateOnly(2028, 5, 8), new DateOnly(2028, 5, 18),
            150m, 20m, 10m, 30m, "EUR", ["Hotel"], 1, 8);

        // Act
        var (result, serializationFailures, handlerAttempts, tourIds, outboxEvents) =
            await TourCreationRetryPostgreSqlScenario.CreateTour(database.ConnectionString, command, ct);

        // Assert
        serializationFailures.ShouldBe(1);
        result.IsSuccess.ShouldBeTrue(result.ErrorDetails?.Detail);
        handlerAttempts.ShouldBe(2);
        tourIds.ShouldHaveSingleItem().ShouldBe(result.Value);
        var created = outboxEvents.ShouldHaveSingleItem();
        created.AdminTourId.ShouldBe(result.Value);
        created.Identifier.ShouldBe(command.Identifier);
    }
}
