using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Playwright;

namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Helpers;

/// <summary>
/// Records browser-side events (console, page errors, failed requests, WebSocket lifecycle) so a
/// timed-out wait can report what the browser and the Blazor circuit were doing.
/// </summary>
internal sealed class PageDiagnostics
{
    private const int MaxHtmlLength = 4000;
    private readonly ConcurrentQueue<string> events = new();
    private readonly Stopwatch elapsed = Stopwatch.StartNew();

    /// <summary>Starts recording console, page error, failed request, response and WebSocket events of the page.</summary>
    /// <param name="page">The page to observe.</param>
    public PageDiagnostics(IPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        page.Console += (_, message) => Record($"console.{message.Type}: {message.Text}");
        page.PageError += (_, error) => Record($"pageerror: {error}");
        page.RequestFailed += (_, request) => Record($"requestfailed: {request.Method} {request.Url} {request.Failure}");
        page.Response += (_, response) =>
        {
            if (response.Status >= 400)
            {
                Record($"response {response.Status}: {response.Request.Method} {response.Url}");
            }
        };
        page.WebSocket += (_, socket) =>
        {
            Record($"websocket open: {socket.Url}");
            socket.SocketError += (_, error) => Record($"websocket error: {socket.Url} {error}");
            socket.Close += (_, _) => Record($"websocket close: {socket.Url}");
        };
    }

    /// <summary>Builds a report of the page URL, the recorded events and the first 4000 characters of the page HTML.</summary>
    public async Task<string> Describe(IPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        var report = new StringBuilder();
        report.AppendLine(CultureInfo.InvariantCulture, $"url: {page.Url}");
        foreach (var entry in events)
        {
            report.AppendLine(entry);
        }

        try
        {
            var html = await page.ContentAsync();
            report.AppendLine(CultureInfo.InvariantCulture, $"html ({html.Length} chars):");
            report.AppendLine(html.Length > MaxHtmlLength ? html[..MaxHtmlLength] : html);
        }
        catch (PlaywrightException exception)
        {
            report.AppendLine(CultureInfo.InvariantCulture, $"html unavailable: {exception.Message}");
        }

        return report.ToString();
    }

    private void Record(string entry)
    {
        events.Enqueue(string.Create(CultureInfo.InvariantCulture, $"+{elapsed.Elapsed.TotalSeconds:F1}s {entry}"));
    }
}
