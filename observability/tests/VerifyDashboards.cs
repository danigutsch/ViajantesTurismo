#:package Microsoft.Playwright
#:package PandaBear.SharedKernel.Documentation
#:property TreatWarningsAsErrors=false
#:property PublishAot=false
#:property JsonSerializerIsReflectionEnabledByDefault=true
#:property NoWarn=$(NoWarn);CA2007;CA1849

// Renders every provisioned Grafana dashboard in headless Chromium and fails when a panel that must
// carry data shows "No data" or a query error. Screenshots are written for human review.
//
// Run: dotnet run observability/tests/VerifyDashboards.cs -- <grafanaUrl> <outputDirectory>

using System.Text.Json;
using Microsoft.Playwright;
using PandaBear.Documentation;

var grafana = args[0].TrimEnd('/');
var output = Directory.CreateDirectory(args[1]).FullName;
var repositoryRoot = new DirectoryInfo(Directory.GetCurrentDirectory());
while (repositoryRoot is not null && !File.Exists(Path.Combine(repositoryRoot.FullName, "ViajantesTurismo.slnx")))
{
    repositoryRoot = repositoryRoot.Parent;
}

if (repositoryRoot is null)
{
    Console.Error.WriteLine("Run from inside the repository.");
    return 2;
}

var dashboardDirectory = Path.Combine(repositoryRoot.FullName, "observability", "grafana", "dashboards");

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
var page = await browser.NewPageAsync(new BrowserNewPageOptions { ViewportSize = new ViewportSize { Width = 1600, Height = 4200 } });

var failures = new List<string>();

foreach (var file in Directory.EnumerateFiles(dashboardDirectory, "*.json").Order(StringComparer.Ordinal))
{
    using var document = JsonDocument.Parse(await File.ReadAllTextAsync(file));
    var root = document.RootElement;
    var uid = root.GetProperty("uid").GetString()!;
    var mustHaveData = root.GetProperty("panels").EnumerateArray()
        .Where(p => p.GetProperty("type").GetString() is not ("text" or "row")
            && (p.TryGetProperty("verify", out var v) ? v.GetString() : "data") == "data")
        .Select(p => p.GetProperty("title").GetString()!)
        .ToHashSet(StringComparer.Ordinal);

    await page.GotoAsync($"{grafana}/d/{uid}?orgId=1&from=now-1h&to=now&kiosk");
    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    await page.WaitForTimeoutAsync(6000);
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, $"{uid}.png"), FullPage = false });

    var states = await page.EvaluateAsync<JsonElement>("""
        () => [...document.querySelectorAll('[data-testid^="data-testid Panel header"]')].map(h => {
            const title = h.getAttribute('data-testid').replace('data-testid Panel header ', '');
            const panel = h.closest('[data-panelid]') ?? h.parentElement?.parentElement ?? h;
            const text = panel.innerText ?? '';
            const noData = /No data|No logs found/i.test(text);
            const drawn = noData || !!panel.querySelector('canvas, table, [role="row"], [class*="logs"]') || text.trim().length > title.length + 8;
            return { title, drawn, noData, error: !!panel.querySelector('[data-testid="data-testid Panel status error"]') };
        })
        """);

    var observations = states.EnumerateArray().Select(state => new GrafanaPanelObservation(
        state.GetProperty("title").GetString()!,
        state.GetProperty("drawn").GetBoolean(),
        state.GetProperty("noData").GetBoolean(),
        state.GetProperty("error").GetBoolean()));
    foreach (var result in GrafanaVerification.CheckPanels(mustHaveData, observations))
    {
        if (result.Observation is { } observation)
        {
            Console.WriteLine($"{(result.Passed ? "ok  " : "FAIL")} {uid}/{result.Title} drawn={observation.Drawn} noData={observation.NoData} error={observation.QueryError}");
        }
        else
        {
            Console.WriteLine($"FAIL {uid}/{result.Title} panel not rendered");
        }

        if (!result.Passed)
        {
            failures.Add($"{uid}/{result.Title}");
        }
    }
}

Console.WriteLine(failures.Count == 0 ? "All dashboards rendered with data." : $"{failures.Count} panel(s) failed.");
return failures.Count == 0 ? 0 : 1;
