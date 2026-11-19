using Microsoft.EntityFrameworkCore;
using PandaBear.Results;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Tours.DeleteTour;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.HostedTests.Admin.Infrastructure.CommitAmbiguity;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Tours;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class TourRetirementPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
{
    private static readonly DateTimeOffset RequestedAt = new(2026, 11, 19, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task Retirement_replay_after_commit_preserves_the_original_audit_and_outbox_event()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);
        var observer = new DbContextOptionsBuilder<AdminWriteDbContext>().UseNpgsql(database.ConnectionString).Options;
        var interceptor = new CommittedTimeoutInterceptor(observer,
            db => db.TourRetirementAuditRecords.Select(record => record.RetirementId));
        var (scenario, tourId, _) = await TourRetirementPostgreSqlScenario.Create(database.ConnectionString, ct, interceptor);
        await using var ownedScenario = scenario;
        var command = new DeleteTourCommand(tourId, "opaque-admin", RequestedAt);
        var attempts = 0;

        // Act
        interceptor.Arm();
        var result = await scenario.Retire(command, ct, current =>
        {
            attempts++;
            return Task.FromResult(current);
        });
        var pending = await scenario.Read(tourId, ct);
        var eventId = pending.Events.ShouldHaveSingleItem().RetirementId;
        await scenario.Acknowledge(tourId, Guid.CreateVersion7(), RequestedAt.AddMinutes(1), ct);
        var mismatched = await scenario.Read(tourId, ct);
        await scenario.Acknowledge(tourId, eventId, RequestedAt.AddMinutes(2), ct);
        await scenario.Acknowledge(tourId, eventId, RequestedAt.AddMinutes(3), ct);
        var completed = await scenario.Read(tourId, ct);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        attempts.ShouldBe(2);
        pending.Active.ShouldBeNull();
        pending.ActiveList.ShouldBeEmpty();
        pending.Historical.ShouldNotBeNull();
        pending.Status.ShouldNotBeNull();
        pending.Status.RetiredAt.ShouldBe(RequestedAt);
        pending.Status.CompletedAt.ShouldBeNull();
        mismatched.Status.ShouldNotBeNull();
        mismatched.Status.CompletedAt.ShouldBeNull();
        completed.Status.ShouldNotBeNull();
        completed.Status.CompletedAt.ShouldBe(RequestedAt.AddMinutes(2));
        var audit = completed.Audits.ShouldHaveSingleItem();
        audit.RetirementId.ShouldBe(interceptor.CommittedIds.ShouldHaveSingleItem());
        audit.ActorId.ShouldBe("opaque-admin");
        audit.OccurredAtUtc.ShouldBe(RequestedAt.UtcDateTime);
        audit.Outcome.ShouldBe(ResultStatus.Ok);
        var notification = completed.Events.ShouldHaveSingleItem();
        notification.AdminTourId.ShouldBe(tourId);
        notification.RetirementId.ShouldBe(audit.RetirementId);
        notification.EventId.ShouldBe(audit.RetirementId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task An_unsuccessful_transaction_rolls_back_the_retirement_audit_and_outbox(bool throws)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);
        var (scenario, tourId, _) = await TourRetirementPostgreSqlScenario.Create(database.ConnectionString, ct);
        await using var ownedScenario = scenario;
        var command = new DeleteTourCommand(tourId, "opaque-admin", RequestedAt);

        // Act
        var exception = await Record.ExceptionAsync(async () =>
        {
            var result = await scenario.Retire(command, ct, _ => throws
                ? throw new InvalidOperationException("Test-owned failure after retirement was saved.")
                : Task.FromResult(Result.Conflict("Test-owned conflict after retirement was saved.")));
            result.Status.ShouldBe(ResultStatus.Conflict);
        });
        var persisted = await scenario.Read(tourId, ct);

        // Assert
        if (throws)
        {
            exception.ShouldBeOfType<InvalidOperationException>();
        }
        else
        {
            exception.ShouldBeNull();
        }

        persisted.Active.ShouldNotBeNull();
        persisted.Status.ShouldBeNull();
        persisted.Audits.ShouldBeEmpty();
        persisted.Events.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Booking_creation_and_retirement_resolve_their_race_under_the_tour_lock(bool retirementFirst)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);
        var (scenario, tourId, customerId) = await TourRetirementPostgreSqlScenario.Create(database.ConnectionString, ct);
        await using var ownedScenario = scenario;
        var command = new DeleteTourCommand(tourId, "opaque-admin", RequestedAt);

        // Act
        var (retired, booked) = await scenario.RaceBookingAndRetirement(command, customerId, retirementFirst, ct);
        var persisted = await scenario.Read(tourId, ct);

        // Assert
        if (retirementFirst)
        {
            retired.IsSuccess.ShouldBeTrue();
            booked.Status.ShouldBe(ResultStatus.Conflict);
            persisted.Status.ShouldNotBeNull();
            persisted.BookingIds.ShouldBeEmpty();
            persisted.Audits.ShouldHaveSingleItem();
            persisted.Events.ShouldHaveSingleItem();
        }
        else
        {
            booked.IsSuccess.ShouldBeTrue();
            retired.Status.ShouldBe(ResultStatus.Conflict);
            persisted.Status.ShouldBeNull();
            persisted.BookingIds.ShouldBe([booked.Value]);
            persisted.Audits.ShouldBeEmpty();
            persisted.Events.ShouldBeEmpty();
        }
    }
}
