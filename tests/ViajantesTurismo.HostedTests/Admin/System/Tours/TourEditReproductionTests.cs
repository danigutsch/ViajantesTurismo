using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.Playwright.Xunit.v3;
using Microsoft.Playwright;
using PandaBear.Testing.Playwright;
using PandaBear.Testing.Snapshots;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Contracts.Http;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Fixtures;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure;
using ViajantesTurismo.Testing.Reproduction;

namespace ViajantesTurismo.HostedTests.Admin.System.Tours;

/// <summary>Repeats a protected Tour edit with fresh browser state and an independently owned tour each time.</summary>
[Trait(TestTraitNames.HostName, TestTraits.AspireHost)]
[Trait(TestTraitNames.SurfaceName, TestTraits.AdminSurface)]
[TestScope(TestScope.System)]
public sealed class TourEditReproductionTests(AspireSystemTestFixture fixture)
    : BrowserTest, IClassFixture<AspireSystemTestFixture>
{
    [Theory]
    [InlineData(true, TourEditReproductionScenario.Accepted, 1)]
    [InlineData(true, TourEditReproductionScenario.Accepted, 2)]
    [InlineData(false, TourEditReproductionScenario.Rejected, 1)]
    [InlineData(false, TourEditReproductionScenario.Rejected, 2)]
    public async Task Repeats_authenticated_edit_outcomes_and_writes_approved_artifacts(
        bool validDuration, string outcomeCode, int repetition)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var specification = TourEditReproductionScenario.Specification;
        var outcome = specification.AllowedOutcomes.ShouldHaveSingleItem(candidate => candidate.OutcomeCode == outcomeCode);
        using var scratch = new TemporaryDirectory();
        var destination = ReproductionFixtureEnvironment.GetLocalResultsDirectory() ?? scratch.Path;
        var selectedEnd = validDuration ? TourEditReproductionScenario.ValidEnd : TourEditReproductionScenario.ShortEnd;
        Assertions.SetDefaultExpectTimeout(30000);

        using var activity = new Activity(specification.Scenario);
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.SetParentId(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
        activity.Start();
        var startedUtc = TimeProvider.System.GetUtcNow();
        var stopwatch = Stopwatch.StartNew();
        var attemptId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var originalName = $"Synthetic tour schedule {suffix}";
        var updatedName = $"Synthetic updated tour schedule {suffix}";
        using var api = await fixture.CreateApiClient(ct);
        var tour = await TourEditReproductionScenario.Seed(api, originalName);
        var editAddress = new Uri(fixture.WebAppUrl, $"/edittour/{tour.Id}");
        await using var context = await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        var update = page.GetButton("Update Tour");

        // Act
        await TourEditReproductionScenario.OpenProtectedEdit(page, editAddress);

        // Assert
        await Expect(page.Locator("#username")).ToBeVisibleAsync();
        await Expect(update).Not.ToBeVisibleAsync();

        // Act
        await TourEditReproductionScenario.SignInAndOpenEdit(page, fixture, editAddress);
        var name = page.GetByLabel("Tour Name", new PageGetByLabelOptions { Exact = true });
        var end = page.GetByLabel("End Date", new PageGetByLabelOptions { Exact = true });

        // Assert
        await Expect(page.GetHeading("Edit Tour")).ToBeVisibleAsync();
        await Expect(name).ToHaveValueAsync(originalName);
        await Expect(end).ToHaveValueAsync(TourEditReproductionScenario.OriginalEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        await page.ShouldHaveNoUnlabelledControls($"Tour form controls in fresh attempt {repetition} have labels");

        // Act
        await name.FillAndExpectValue(updatedName);
        await end.FillAndExpectValue(selectedEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        await update.ClickAsync();

        // Assert
        if (validDuration)
        {
            await page.ExpectPath(fixture.WebAppUrl, $"/tours/{tour.Id}");
            await Expect(page.GetHeading(updatedName)).ToBeVisibleAsync();
        }
        else
        {
            var summary = page.GetByRole(AriaRole.List).Filter(
                new LocatorFilterOptions { HasText = TourEditReproductionScenario.ValidationMessage });
            await Expect(summary).ToBeVisibleAsync();
            await Expect(update).ToBeEnabledAsync();
            await page.ExpectPath(editAddress);
        }

        var observedTour = (await new ToursApiClient(api).GetTourById(tour.Id, ct)).Value.ShouldNotBeNull();
        observedTour.Name.ShouldBe(validDuration ? updatedName : originalName);
        observedTour.EndDate.ShouldBe(validDuration ? selectedEnd : TourEditReproductionScenario.OriginalEnd);

        var manifest = new ReproductionManifest(specification.Revision, specification.Scenario, attemptId, [], outcome,
            startedUtc, (int)stopwatch.ElapsedMilliseconds);
        var path = await ReproductionManifestCodec.Write(destination, specification, manifest, ct);
        var artifact = await ReproductionManifestCodec.Read(path, specification, ct);
        artifact.AttemptId.ShouldBe(attemptId);
        artifact.TraceIds.ShouldBeEmpty();
        artifact.Outcome.OutcomeCode.ShouldBe(outcomeCode);
        artifact.Outcome.ResourceStates.Keys.Order(StringComparer.Ordinal).ToArray().ShouldBe(["admin-api", "management-web"]);

        // Act
        var json = JsonNode.Parse(await File.ReadAllTextAsync(path, ct)).ShouldNotBeNull().AsObject();
        json.Add("storageState", new JsonObject { ["cookies"] = new JsonArray("synthetic-browser-cookie") });
        var contaminated = Path.Combine(scratch.Path, $"{attemptId:N}-contaminated.json");
        await File.WriteAllTextAsync(contaminated, json.ToJsonString(), ct);
        Func<Task> readContaminated = async () => _ = await ReproductionManifestCodec.Read(contaminated, specification, ct);

        // Assert
        var error = await readContaminated.ShouldThrow<InvalidDataException>();
        error.Message.ShouldBe("The reproduction manifest does not match the approved fixture.");
    }
}
