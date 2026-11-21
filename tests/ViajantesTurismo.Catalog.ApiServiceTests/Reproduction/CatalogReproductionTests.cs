using PandaBear.Testing;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Testing.Reproduction;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Reproduction;

/// <summary>Reproduces Catalog management and public outcomes with separately owned state.</summary>
[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class CatalogReproductionTests
{
    [Fact]
    public async Task Independent_resets_reject_a_stale_presentation_without_overwriting_the_committed_edit()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var specification = CatalogReproductionScenario.StaleWrite;
        using var scratch = new TemporaryDirectory();
        var resultsDirectory = ReproductionManifestCodec.GetLocalResultsDirectory() ?? scratch.Path;

        // Act
        var first = await CatalogReproductionScenario.RunManagement(ct);
        var second = await CatalogReproductionScenario.RunManagement(ct);

        // Assert
        first.TourId.ShouldNotBe(second.TourId);
        first.Manifest.AttemptId.ShouldNotBe(second.Manifest.AttemptId);
        first.Manifest.TraceIds.ShouldHaveSingleItem().ShouldNotBe(second.Manifest.TraceIds.ShouldHaveSingleItem());
        foreach (var run in new[] { first, second })
        {
            run.Forbidden.ShouldBe(HttpStatusCode.Forbidden);
            run.Invalid.ShouldBe(HttpStatusCode.BadRequest);
            run.Committed.ShouldBe(HttpStatusCode.OK);
            run.Stale.ShouldBe(HttpStatusCode.Conflict);
            run.Read.ShouldBe(HttpStatusCode.OK);
            var tour = run.Presentation.ShouldNotBeNull();
            tour.Id.ShouldBe(run.TourId);
            tour.Version.ShouldBe(2);
            tour.Title.ShouldBe(CatalogReproductionScenario.CommittedTourTitle);
            run.ObservedRequests.ShouldBe(5);

            var path = await ReproductionManifestCodec.Write(resultsDirectory, specification, run.Manifest, ct);
            var artifact = await ReproductionManifestCodec.Read(path, specification, ct);
            var json = await File.ReadAllTextAsync(path, ct);
            artifact.AttemptId.ShouldBe(run.Manifest.AttemptId);
            artifact.TraceIds.ShouldBe(run.Manifest.TraceIds);
            artifact.Outcome.OutcomeCode.ShouldBe(CatalogReproductionScenario.StaleWriteOutcome);
            artifact.Outcome.ResourceStates.Keys.ShouldBe(["catalog-api"]);
            json.ShouldNotContain(CatalogReproductionScenario.StaleTourTitle, StringComparison.Ordinal);
            json.ShouldNotContain(CatalogReproductionScenario.CommittedTourTitle, StringComparison.Ordinal);
            json.ShouldNotContain(run.TourId.ToString("D"), StringComparison.Ordinal);
            json.ShouldNotContain(run.TourId.ToString("N"), StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Independent_anonymous_reads_return_the_requested_approved_locale_without_recording_content()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var specification = CatalogReproductionScenario.LocalizedRead;
        using var scratch = new TemporaryDirectory();
        var resultsDirectory = ReproductionManifestCodec.GetLocalResultsDirectory() ?? scratch.Path;

        // Act
        var first = await CatalogReproductionScenario.RunLocalizedRead(ct);
        var second = await CatalogReproductionScenario.RunLocalizedRead(ct);

        // Assert
        first.ContentKey.ShouldNotBe(second.ContentKey);
        first.Manifest.AttemptId.ShouldNotBe(second.Manifest.AttemptId);
        first.Manifest.TraceIds.ShouldHaveSingleItem().ShouldNotBe(second.Manifest.TraceIds.ShouldHaveSingleItem());
        foreach (var run in new[] { first, second })
        {
            run.Status.ShouldBe(HttpStatusCode.OK);
            var variant = run.Content.ShouldNotBeNull();
            variant.Language.ShouldBe(PublicContentLanguageTags.PortugueseBrazil);
            variant.Title.ShouldBe(CatalogReproductionScenario.LocalizedTitle);
            variant.Body.ShouldBe(CatalogReproductionScenario.LocalizedBody);
            variant.RequiresHumanReview.ShouldBeFalse();
            run.ObservedRequests.ShouldBe(1);

            var path = await ReproductionManifestCodec.Write(resultsDirectory, specification, run.Manifest, ct);
            var artifact = await ReproductionManifestCodec.Read(path, specification, ct);
            var json = await File.ReadAllTextAsync(path, ct);
            artifact.AttemptId.ShouldBe(run.Manifest.AttemptId);
            artifact.TraceIds.ShouldBe(run.Manifest.TraceIds);
            artifact.Outcome.OutcomeCode.ShouldBe(CatalogReproductionScenario.LocalizedReadOutcome);
            artifact.Outcome.ResourceStates.Keys.ShouldBe(["catalog-api"]);
            json.ShouldNotContain(run.ContentKey, StringComparison.Ordinal);
            json.ShouldNotContain(CatalogReproductionScenario.LocalizedTitle, StringComparison.Ordinal);
            json.ShouldNotContain(CatalogReproductionScenario.LocalizedBody, StringComparison.Ordinal);
            json.ShouldNotContain(CatalogReproductionScenario.HeaderMarker, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData(CatalogReproductionScenario.StaleTourTitle)]
    [InlineData(CatalogReproductionScenario.LocalizedBody)]
    [InlineData(CatalogReproductionScenario.HeaderMarker)]
    public async Task A_request_or_response_value_cannot_become_an_artifact_outcome(string marker)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var specification = CatalogReproductionScenario.LocalizedRead;
        using var scratch = new TemporaryDirectory();
        var outcome = specification.AllowedOutcomes.ShouldHaveSingleItem() with
        {
            OutcomeCode = marker
        };
        var invalid = new ReproductionManifest(specification.Revision, specification.Scenario, Guid.NewGuid(), [],
            outcome, TimeProvider.System.GetUtcNow(), 0);

        // Act
        Func<Task> write = async () => await ReproductionManifestCodec.Write(scratch.Path, specification, invalid, ct);

        // Assert
        var rejected = await write.ShouldThrow<InvalidDataException>();
        rejected.Message.ShouldNotContain(marker, StringComparison.Ordinal);
        Directory.EnumerateFileSystemEntries(scratch.Path).ShouldBeEmpty();
    }
}
