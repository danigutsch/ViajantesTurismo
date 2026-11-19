using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Customers.CreateCustomer;
using ViajantesTurismo.Admin.Application.Tours.CreateTour;
using ViajantesTurismo.Admin.Testing.Builders;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.CommitAmbiguity;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CreateCommandCommitAmbiguityPostgreSqlTests(PostgreSqlTestServerFixture fixture)
    : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Retrying_after_the_tour_commit_returns_the_original_tour()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);
        var command = new CreateTourCommand(
            "COMMITTEDTOUR", "Committed tour", new DateOnly(2028, 5, 8), new DateOnly(2028, 5, 18),
            150m, 20m, 10m, 30m, "EUR", ["Hotel"], 1, 8);

        // Act
        var (result, attempts, committed, persisted) =
            await CreateCommandCommitAmbiguityPostgreSqlScenario.CreateTour(database.ConnectionString, command, ct);

        // Assert
        var originalId = committed.ShouldHaveSingleItem();
        attempts.ShouldBe(2);
        persisted.ShouldBe([originalId]);
        result.IsSuccess.ShouldBeTrue(result.ErrorDetails?.Detail);
        result.Value.ShouldBe(originalId);
    }

    [Fact]
    public async Task Retrying_after_the_customer_commit_returns_the_original_customer()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);
        var dto = DtoBuilders.BuildCreateCustomerDto(email: "committed-customer@example.test");
        var command = new CreateCustomerCommand(dto.PersonalInfo, dto.IdentificationInfo, dto.ContactInfo,
            dto.Address, dto.PhysicalInfo, dto.AccommodationPreferences, dto.EmergencyContact, dto.MedicalInfo);

        // Act
        var (result, attempts, committed, persisted) =
            await CreateCommandCommitAmbiguityPostgreSqlScenario.CreateCustomer(database.ConnectionString, command, ct);

        // Assert
        var originalId = committed.ShouldHaveSingleItem();
        attempts.ShouldBe(2);
        persisted.ShouldBe([originalId]);
        result.IsSuccess.ShouldBeTrue(result.ErrorDetails?.Detail);
        result.Value.ShouldBe(originalId);
    }
}
