#:package PandaBear.SharedKernel.Documentation
#:property PublishAot=false

// Checks the file-provisioned dashboard queries and runs them through the Grafana datasource proxy.
// Panels with verify "data" must return series after smoke traffic.
// Panels with "idle-ok" only need a valid query.
//
// Run: dotnet run observability/tests/VerifyQueries.cs -- <grafanaUrl> [--dashboard <uid>]
// Optional Basic credentials come from GRAFANA_USER and GRAFANA_PASSWORD and are only sent over https or loopback.

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SharedKernel.Documentation;

var dashboardUid = args is [_, "--dashboard", var requestedUid] ? requestedUid : null;
if ((args.Length != 1 && string.IsNullOrWhiteSpace(dashboardUid))
    || !Uri.TryCreate(args[0], UriKind.Absolute, out var grafanaUri)
    || grafanaUri.Scheme is not ("http" or "https"))
{
    await Console.Error.WriteLineAsync("Usage: dotnet run observability/tests/VerifyQueries.cs -- <http(s)://grafana-host:port> [--dashboard <uid>]")
        .ConfigureAwait(false);
    return 2;
}

var grafana = grafanaUri.ToString().TrimEnd('/');
var repositoryRoot = new DirectoryInfo(Directory.GetCurrentDirectory());
while (repositoryRoot is not null && !File.Exists(Path.Combine(repositoryRoot.FullName, "ViajantesTurismo.slnx")))
{
    repositoryRoot = repositoryRoot.Parent;
}

if (repositoryRoot is null)
{
    await Console.Error.WriteLineAsync("Run from inside the repository.").ConfigureAwait(false);
    return 2;
}

var dashboardDirectory = Path.Combine(repositoryRoot.FullName, "observability", "grafana", "dashboards");
var files = Directory.EnumerateFiles(dashboardDirectory, "*.json")
    .Where(file => dashboardUid is null || string.Equals(Path.GetFileNameWithoutExtension(file), dashboardUid, StringComparison.Ordinal))
    .Order(StringComparer.Ordinal)
    .ToArray();
if (files.Length == 0)
{
    await Console.Error.WriteLineAsync(dashboardUid is null
        ? "No checked-in dashboard files were found."
        : $"No checked-in dashboard has UID '{dashboardUid}'.").ConfigureAwait(false);
    return 2;
}

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var user = Environment.GetEnvironmentVariable("GRAFANA_USER");
var password = Environment.GetEnvironmentVariable("GRAFANA_PASSWORD");
if (!string.IsNullOrEmpty(user) && (grafanaUri.Scheme == "https" || grafanaUri.IsLoopback))
{
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
        "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}")));
}

var failures = new List<string>();
var now = TimeProvider.System.GetUtcNow().ToUnixTimeSeconds();

foreach (var file in files)
{
    using var document = JsonDocument.Parse(await File.ReadAllTextAsync(file).ConfigureAwait(false));
    var uid = document.RootElement.GetProperty("uid").GetString()!;
    using var response = await Get($"{grafana}/api/dashboards/uid/{Uri.EscapeDataString(uid)}").ConfigureAwait(false);
    var live = response.RootElement;
    var provisionedFromFile = live.TryGetProperty("dashboard", out var provisioned)
        && live.TryGetProperty("meta", out var meta)
        && meta.TryGetProperty("provisioned", out var fromFile)
        && fromFile.ValueKind == JsonValueKind.True;
    GrafanaQueryTarget[] targets = provisionedFromFile ? [.. ReadTargets(document.RootElement)] : [];
    var status = GrafanaVerification.CheckProvisioning(provisionedFromFile, targets,
        provisionedFromFile ? ReadTargets(provisioned) : []);
    if (status == GrafanaProvisioningStatus.NotProvisioned)
    {
        await Console.Error.WriteLineAsync($"FAIL {uid} requires a file-provisioned dashboard.").ConfigureAwait(false);
        failures.Add(uid);
        continue;
    }

    if (status == GrafanaProvisioningStatus.TargetsDiffer)
    {
        await Console.Error.WriteLineAsync($"FAIL {uid} has panel targets that differ from the checked-in dashboard.").ConfigureAwait(false);
        failures.Add(uid);
        continue;
    }

    foreach (var target in targets)
    {
        var observation = await RunTarget(target).ConfigureAwait(false);
        var passed = observation.Passes(requireData: target.VerificationMode != "idle-ok");
        await Console.Out.WriteLineAsync($"{(passed ? "ok  " : "FAIL")} {uid}/{target.Title} [{target.RefId}] series={observation.SeriesCount} mode={target.VerificationMode}")
            .ConfigureAwait(false);
        if (!passed)
        {
            failures.Add($"{uid}/{target.Title}");
        }
    }
}

await Console.Out.WriteLineAsync(failures.Count == 0 ? "All panel queries verified." : $"\n{failures.Count} failed verification(s)")
    .ConfigureAwait(false);
return failures.Count == 0 ? 0 : 1;

static IEnumerable<GrafanaQueryTarget> ReadTargets(JsonElement dashboard)
{
    foreach (var panel in dashboard.GetProperty("panels").EnumerateArray())
    {
        if (panel.GetProperty("type").GetString() is "row" or "text")
        {
            continue;
        }

        var panelId = panel.GetProperty("id").GetInt32();
        var title = panel.GetProperty("title").GetString()!;
        var mode = panel.TryGetProperty("verify", out var verify) ? verify.GetString() ?? "data" : "data";
        foreach (var target in panel.GetProperty("targets").EnumerateArray())
        {
            var datasource = target.TryGetProperty("datasource", out var own) ? own : panel.GetProperty("datasource");
            var query = target.TryGetProperty("expr", out var expr) ? expr : target.GetProperty("query");
            yield return new GrafanaQueryTarget(
                panelId,
                title,
                target.GetProperty("refId").GetString()!,
                datasource.GetProperty("type").GetString()!,
                datasource.GetProperty("uid").GetString()!,
                query.GetString()!,
                mode);
        }
    }
}

async Task<GrafanaQueryObservation> RunTarget(GrafanaQueryTarget target)
{
    var proxy = $"{grafana}/api/datasources/proxy/uid/{Uri.EscapeDataString(target.DatasourceUid)}";
    switch (target.DatasourceType)
    {
        case "prometheus":
            var promQuery = target.Query.Replace("$service", ".*", StringComparison.Ordinal)
                .Replace("$__rate_interval", "5m", StringComparison.Ordinal)
                .Replace("$__interval", "1m", StringComparison.Ordinal);
            using (var response = await Get($"{proxy}/api/v1/query?query={Uri.EscapeDataString(promQuery)}").ConfigureAwait(false))
            {
                return new GrafanaQueryObservation(response.RootElement.GetProperty("status").GetString() == "success",
                    response.RootElement.GetProperty("data").GetProperty("result").GetArrayLength());
            }

        case "loki":
            var lokiQuery = target.Query.Replace("$__auto", "5m", StringComparison.Ordinal)
                .Replace("$__range", "1h", StringComparison.Ordinal);
            var lokiUrl = $"{proxy}/loki/api/v1/query_range?query={Uri.EscapeDataString(lokiQuery)}"
                + $"&start={(now - 3600) * 1_000_000_000}&end={now * 1_000_000_000}&limit=1";
            using (var response = await Get(lokiUrl).ConfigureAwait(false))
            {
                return new GrafanaQueryObservation(response.RootElement.GetProperty("status").GetString() == "success",
                    response.RootElement.GetProperty("data").GetProperty("result").GetArrayLength());
            }

        case "tempo":
            var tempoUrl = $"{proxy}/api/search?q={Uri.EscapeDataString(target.Query)}&start={now - 3600}&end={now}&limit=1";
            using (var response = await Get(tempoUrl).ConfigureAwait(false))
            {
                var failed = response.RootElement.TryGetProperty("status", out var status) && status.GetString() == "error";
                return new GrafanaQueryObservation(!failed, response.RootElement.TryGetProperty("traces", out var traces) ? traces.GetArrayLength() : 0);
            }

        default:
            throw new InvalidOperationException($"Unknown datasource type {target.DatasourceType}.");
    }
}

async Task<JsonDocument> Get(string url)
{
    var address = new Uri(url, UriKind.Absolute);
    using var response = await http.GetAsync(address).ConfigureAwait(false);
    if (response.IsSuccessStatusCode)
    {
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
    }

    await Console.Error.WriteLineAsync($"HTTP {(int)response.StatusCode} from {address.AbsolutePath}.").ConfigureAwait(false);
    return JsonDocument.Parse("""{"status":"error","data":{"result":[]},"traces":[]}""");
}
