using Microsoft.Playwright;
using SharedKernel.Testing.Playwright;

namespace ViajantesTurismo.HostedTests.Admin.System.PostTransportValidation;

/// <summary>Provides the pinned browser engine input and adds route context to smoke failures.</summary>
internal static class BrowserAccessibilitySmokeAsset
{
    private const string EnginePathVariable = "VT_WCAG_ENGINE_PATH";
    private const string EngineSha256 = "1fdeffc9579a41d2e919222e52a4f5057e54b60f8ea002a6a8004737ffd7a249";

    /// <summary>Gets whether the caller supplied the path to the local browser bundle.</summary>
    public static bool IsConfigured => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnginePathVariable));

    /// <summary>Checks the current page and reports confirmed violations for the supplied route.</summary>
    /// <param name="page">The browser page after navigation.</param>
    /// <param name="route">The static route label to include in diagnostics.</param>
    public static async Task Verify(IPage page, string route)
    {
        var enginePath = Environment.GetEnvironmentVariable(EnginePathVariable)
            ?? throw new InvalidOperationException($"Set {EnginePathVariable} to the documented browser engine bundle.");

        try
        {
            await page.ShouldHaveNoEqualAccessViolations(enginePath, EngineSha256);
        }
        catch (PlaywrightException exception)
        {
            throw new PlaywrightException($"WCAG A/AA smoke scan for {route}: {exception.Message}", exception);
        }
    }
}
