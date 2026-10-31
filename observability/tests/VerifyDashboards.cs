#:package Microsoft.Playwright
#:property TreatWarningsAsErrors=false
#:property PublishAot=false
#:property JsonSerializerIsReflectionEnabledByDefault=true
#:property NoWarn=$(NoWarn);CA2007

// Renders every provisioned Grafana dashboard in headless Chromium and fails when a panel that must
// carry data shows "No data" or a query error. Screenshots are written for human review.
//
// Run: dotnet run observability/tests/VerifyDashboards.cs -- <grafanaUrl> <outputDirectory>

using System.Text.Json;
using Microsoft.Playwright;

var grafana = args[0].TrimEnd('/');
var output = Directory.CreateDirectory(args[1]).FullName;
var dashboardDirectory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "observability", "grafana", "dashboards"));

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
            const drawn = !!panel.querySelector('canvas, svg, table, [data-testid*="data-testid Panel data error message"], [class*="logs"]') || text.trim().length > 0;
            return { title, drawn, noData: /No data/i.test(text), error: !!panel.querySelector('[data-testid="data-testid Panel status error"]') };
        })
        """);

    var seen = new HashSet<string>(StringComparer.Ordinal);
    foreach (var state in states.EnumerateArray())
    {
        var title = state.GetProperty("title").GetString()!;
        seen.Add(title);
        var noData = state.GetProperty("noData").GetBoolean();
        var error = state.GetProperty("error").GetBoolean();
        var drawn = state.GetProperty("drawn").GetBoolean();
        var bad = error || !drawn || (noData && mustHaveData.Contains(title));
        Console.WriteLine($"{(bad ? "FAIL" : "ok  ")} {uid}/{title} drawn={drawn} noData={noData} error={error}");
        if (bad)
        {
            failures.Add($"{uid}/{title}");
        }
    }

    foreach (var missing in mustHaveData.Except(seen))
    {
        Console.WriteLine($"FAIL {uid}/{missing} panel not rendered");
        failures.Add($"{uid}/{missing}");
    }
}

Console.WriteLine(failures.Count == 0 ? "All dashboards rendered with data." : $"{failures.Count} panel(s) failed.");
return failures.Count == 0 ? 0 : 1;
