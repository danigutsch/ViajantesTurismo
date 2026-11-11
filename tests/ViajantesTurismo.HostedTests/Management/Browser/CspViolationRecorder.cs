using System.Collections.Concurrent;
using Microsoft.Playwright;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>
/// Records the Content-Security-Policy violations of a page from its <c>securitypolicyviolation</c> events and its console.
/// Start it before the first navigation.
/// </summary>
internal sealed class CspViolationRecorder
{
    private const string RecordViolations = """
        window.cspViolations = [];
        document.addEventListener('securitypolicyviolation', e => window.cspViolations.push(e.violatedDirective + ' ' + e.blockedURI));
        """;

    private readonly ConcurrentQueue<string> console = new();
    private readonly IPage page;

    private CspViolationRecorder(IPage page)
    {
        this.page = page;
        page.Console += (_, message) =>
        {
            if (message.Text.Contains("Content Security Policy", StringComparison.OrdinalIgnoreCase))
            {
                console.Enqueue(message.Text);
            }
        };
    }

    /// <summary>Starts recording on a page that has not navigated yet.</summary>
    /// <param name="page">The page.</param>
    /// <returns>The recorder.</returns>
    public static async Task<CspViolationRecorder> Start(IPage page)
    {
        var recorder = new CspViolationRecorder(page);
        await page.AddInitScriptAsync(RecordViolations);
        return recorder;
    }

    /// <summary>Gets every violation the current document reported and every policy message on the console.</summary>
    /// <returns>The violations.</returns>
    public async Task<IReadOnlyList<string>> Violations()
    {
        var events = await page.EvaluateAsync<string[]>("() => window.cspViolations");
        return [.. events, .. console];
    }
}
