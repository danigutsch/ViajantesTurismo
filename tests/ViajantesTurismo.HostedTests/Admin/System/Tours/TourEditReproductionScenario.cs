using Microsoft.Playwright;
using SharedKernel.Testing.Playwright;
using SharedKernel.Testing.Snapshots;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Fixtures;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Workflows;
using ViajantesTurismo.Testing.Reproduction;

namespace ViajantesTurismo.HostedTests.Admin.System.Tours;

/// <summary>Defines the owned Tour edit data and approved browser manifest observations.</summary>
internal static class TourEditReproductionScenario
{
    /// <summary>The outcome code for a persisted edit.</summary>
    internal const string Accepted = "edit-persisted";

    /// <summary>The outcome code for rejected duration validation.</summary>
    internal const string Rejected = "short-duration-rejected";

    /// <summary>The visible validation text for a short schedule.</summary>
    internal const string ValidationMessage = "The tour must be at least 5 days long. End date must be more than 5 days after start date.";

    /// <summary>The initial synthetic end date.</summary>
    internal static readonly DateOnly OriginalEnd = new(2035, 5, 18);

    /// <summary>The accepted end date in the edited schedule.</summary>
    internal static readonly DateOnly ValidEnd = new(2035, 5, 21);

    /// <summary>The short schedule end date that validation rejects.</summary>
    internal static readonly DateOnly ShortEnd = new(2035, 5, 15);

    private static readonly ReproductionOutcome[] ApprovedOutcomes =
    [
        new("reproduced", Accepted, new Dictionary<string, string>
        {
            ["management-web"] = "edit-confirmed",
            ["admin-api"] = "updated"
        }),
        new("reproduced", Rejected, new Dictionary<string, string>
        {
            ["management-web"] = "validation-rejected",
            ["admin-api"] = "unchanged"
        })
    ];

    /// <summary>The registered browser scenario and its reviewed outcome combinations.</summary>
    internal static ReproductionSpecification Specification
    {
        get;
    } = new(
        "browser.tour-edit-duration",
        ReproductionFixtureEnvironment.GetSourceRevision(typeof(TourEditReproductionScenario).Assembly),
        ApprovedOutcomes);

    /// <summary>Creates a new tour through the authenticated Admin API using fixed synthetic dates.</summary>
    internal static async Task<GetTourDto> Seed(HttpClient client, string name) =>
        await client.CreateTour(new CreateTourOptions
        {
            Name = name,
            StartDate = new DateTime(2035, 5, 11),
            EndDate = OriginalEnd.ToDateTime(TimeOnly.MinValue)
        });

    /// <summary>Opens the protected route with a fresh context and allows the browser to follow the challenge.</summary>
    internal static async Task OpenProtectedEdit(IPage page, Uri editAddress)
    {
        page.SetDefaultTimeout(30000);
        page.SetDefaultNavigationTimeout(30000);
        try
        {
            await page.GotoAsync(editAddress.AbsoluteUri, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        }
        catch (PlaywrightException)
        {
            throw new InvalidOperationException("The protected Tour edit route could not be opened.");
        }
    }

    /// <summary>Signs in through the existing conformance realm workflow and waits for the interactive form.</summary>
    internal static async Task SignInAndOpenEdit(IPage page, AspireSystemTestFixture fixture, Uri editAddress)
    {
        try
        {
            await new ManagementLoginWorkflow(page).SignIn(fixture.WebAppUrl, fixture.ConformanceUserPassword);
        }
        catch (Exception exception) when (exception is PlaywrightException or TimeoutException or InvalidOperationException)
        {
            throw new InvalidOperationException("The conformance sign-in did not complete.");
        }

        await page.GotoInteractive(editAddress);
    }
}
