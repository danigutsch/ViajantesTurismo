using PandaBear.Results;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
[TestScope(TestScope.Integration)]
public sealed class PaymentTransactionPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Financial_transaction_locks_the_existing_booking_table_and_commits_its_result()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        await using var session = scenario.Open();
        await session.Migrate(ct);
        var tour = EntityBuilders.BuildTour();
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        await session.Seed(tour, ct);

        // Act
        var result = await session.Transaction.Execute(["admin:booking-finance:" + booking.Id.ToString("N")], () => Task.FromResult(Result.Ok(booking.Id)), ct);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(booking.Id);
    }
}
