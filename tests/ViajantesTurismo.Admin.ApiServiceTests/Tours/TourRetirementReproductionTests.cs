using SharedKernel.Testing.Snapshots;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Testing.Reproduction;

namespace ViajantesTurismo.Admin.ApiServiceTests.Tours;

/// <summary>Reproduces the allowed and denied retirement outcomes after independent fixture resets.</summary>
[Trait(TestTraitNames.HostName, TestTraitValues.TestServerHost)]
public sealed class TourRetirementReproductionTests
{
    [Theory]
    [InlineData("Admin", false, HttpStatusCode.Accepted, HttpStatusCode.OK, TourRetirementReproductionScenario.Accepted)]
    [InlineData("Admin", true, HttpStatusCode.Conflict, HttpStatusCode.NotFound, TourRetirementReproductionScenario.Conflict)]
    [InlineData("Reader", false, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden, TourRetirementReproductionScenario.Denied)]
    public async Task Independent_resets_repeat_the_retirement_outcome_and_emit_only_approved_observations(
        string role, bool activeBooking, HttpStatusCode expectedRetirement, HttpStatusCode expectedQuery, string outcomeCode)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scratch = new TemporaryDirectory();
        var specification = TourRetirementReproductionScenario.Specification;
        var outcome = TourRetirementReproductionScenario.Outcome(outcomeCode);
        var resultsDirectory = ReproductionFixtureEnvironment.GetLocalResultsDirectory() ?? scratch.Path;
        var accepted = expectedRetirement == HttpStatusCode.Accepted;

        // Act
        var first = await TourRetirementReproductionScenario.Run(role, activeBooking, ct);
        var second = await TourRetirementReproductionScenario.Run(role, activeBooking, ct);

        // Assert
        first.TourId.ShouldNotBe(second.TourId);
        first.AttemptId.ShouldNotBe(second.AttemptId);
        first.ObservedTrace.ShouldNotBeNull().ShouldNotBe(second.ObservedTrace.ShouldNotBeNull());
        foreach (var run in new[] { first, second })
        {
            run.RetirementStatus.ShouldBe(expectedRetirement);
            run.QueryStatus.ShouldBe(expectedQuery);
            run.IsRetired.ShouldBe(accepted);
            run.Saves.ShouldBe(accepted ? 1 : 0);
            run.BookingStatus.ShouldBe(activeBooking ? BookingStatus.Pending : null);
            if (accepted)
            {
                var retirement = run.Retirement.ShouldNotBeNull();
                retirement.TourId.ShouldBe(run.TourId);
                retirement.RetiredAt.ShouldBe(TourRetirementReproductionScenario.FixtureUtc);
                retirement.CompletedAt.ShouldBeNull();
                run.QueryNoStore.ShouldBeTrue();
            }

            var path = await ReproductionManifestCodec.Write(resultsDirectory, specification, run.Manifest(specification, outcome), ct);
            var artifact = await ReproductionManifestCodec.Read(path, specification, ct);
            artifact.AttemptId.ShouldBe(run.AttemptId);
            artifact.TraceIds.ShouldHaveSingleItem().ShouldBe(run.ObservedTrace.ShouldNotBeNull());
            artifact.Outcome.OutcomeCode.ShouldBe(outcomeCode);
            artifact.Outcome.ResourceStates.Keys.ShouldBe(["admin-api"]);
        }
    }
}
