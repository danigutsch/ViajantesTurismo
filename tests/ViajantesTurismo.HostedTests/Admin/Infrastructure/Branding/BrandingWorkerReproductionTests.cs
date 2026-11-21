using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Branding;
using ViajantesTurismo.Testing.Reproduction;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Branding;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class BrandingWorkerReproductionTests(PostgreSqlTestServerFixture fixture)
    : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task The_worker_retries_a_fenced_delivery_and_settles_the_repeated_event_once()
    {
        // Arrange
        using var scratch = new TemporaryDirectory();
        var specification = BrandingWorkerReproductionScenario.Specification;
        var outcome = BrandingWorkerReproductionScenario.Replayed;
        var resultsDirectory = ReproductionManifestCodec.GetLocalResultsDirectory() ?? scratch.Path;
        var ct = TestContext.Current.CancellationToken;
        var expectedRetry = new BrandingWorkerDelivery(false, 1,
            BrandingWorkerReproductionScenario.InitialTime.AddSeconds(1),
            typeof(BrandingProjectionRecoveringException).FullName, false, false);
        var expectedSettlement = new BrandingWorkerDelivery(true, 1, null, null, false, false);

        // Act
        var first = await BrandingWorkerReproductionScenario.Run(fixture, ct);
        var second = await BrandingWorkerReproductionScenario.Run(fixture, ct);

        // Assert
        first.AttemptId.ShouldNotBe(second.AttemptId);
        first.TraceId.ShouldNotBe(second.TraceId);
        foreach (var run in new[] { first, second })
        {
            run.InitialProjection.ShouldBe((3L, BrandingWorkerReproductionScenario.OriginalBrandName, 1L));
            run.Recovery.ShouldBe(("Recovering", 0L));
            run.Blocked.ShouldBe(expectedRetry);
            run.TraceId.ShouldNotBe(default);
            run.Authorized.ShouldBeTrue();
            run.Retried.ShouldBe(expectedSettlement);
            run.Duplicate.ShouldBe(expectedSettlement);
            run.FinalProjection.ShouldBe((1L, BrandingWorkerReproductionScenario.ReplayBrandName, 2L));
            run.FinalRecovery.ShouldBe(("Ready", 1L));

            var manifest = new ReproductionManifest(specification.Revision, specification.Scenario, run.AttemptId, [run.TraceId],
                outcome, run.StartedUtc, run.DurationMs);
            var path = await ReproductionManifestCodec.Write(resultsDirectory, specification, manifest, ct);
            var persisted = await ReproductionManifestCodec.Read(path, specification, ct);
            var artifactJson = await File.ReadAllTextAsync(path, ct);

            persisted.AttemptId.ShouldBe(run.AttemptId);
            persisted.TraceIds.ShouldHaveSingleItem().ShouldBe(run.TraceId);
            persisted.Outcome.OutcomeCode.ShouldBe(outcome.OutcomeCode);
            artifactJson.ShouldNotContain(BrandingWorkerReproductionScenario.ReplayBrandName, StringComparison.Ordinal);
            artifactJson.ShouldNotContain(BrandingWorkerReproductionScenario.ReplayEventId.ToString("N"), StringComparison.Ordinal);
            artifactJson.ShouldNotContain(BrandingWorkerReproductionScenario.Operator, StringComparison.Ordinal);
            artifactJson.ShouldNotContain(nameof(BrandingProjectionRecoveringException), StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task The_manifest_rejects_a_message_value_as_an_outcome()
    {
        // Arrange
        using var scratch = new TemporaryDirectory();
        var outputDirectory = scratch.Path;
        var specification = BrandingWorkerReproductionScenario.Specification;
        var invalidOutcome = BrandingWorkerReproductionScenario.Replayed with
        {
            OutcomeCode = BrandingWorkerReproductionScenario.ReplayBrandName
        };
        var manifest = new ReproductionManifest(specification.Revision, specification.Scenario, Guid.NewGuid(), [],
            invalidOutcome, TimeProvider.System.GetUtcNow(), 0);
        var ct = TestContext.Current.CancellationToken;

        // Act
        Func<Task> exportPayload = () => ReproductionManifestCodec.Write(outputDirectory, specification, manifest, ct);

        // Assert
        await exportPayload.ShouldThrow<InvalidDataException>();
        Directory.EnumerateFiles(outputDirectory).ShouldBeEmpty();
    }
}
