#:property TreatWarningsAsErrors=false
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA2007;CA2234;CA1849;CA1307

// Runs every dashboard panel query through the Grafana datasource proxy. Panels with verify "data" must
// return series after smoke traffic; panels with "idle-ok" only need a valid query.
//
// Run: dotnet run observability/tests/VerifyQueries.cs -- <grafanaUrl>
// Optional Basic credentials come from GRAFANA_USER and GRAFANA_PASSWORD and are only sent over https or loopback.

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

if (args.Length != 1 || !Uri.TryCreate(args[0], UriKind.Absolute, out var grafanaUri) || grafanaUri.Scheme is not ("http" or "https"))
{
    Console.Error.WriteLine("Usage: dotnet run observability/tests/VerifyQueries.cs -- <http(s)://grafana-host:port>");
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
    Console.Error.WriteLine("Run from inside the repository.");
    return 2;
}

var dashboardDirectory = Path.Combine(repositoryRoot.FullName, "observability", "grafana", "dashboards");

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var user = Environment.GetEnvironmentVariable("GRAFANA_USER");
var password = Environment.GetEnvironmentVariable("GRAFANA_PASSWORD");
if (!string.IsNullOrEmpty(user) && (grafanaUri.Scheme == "https" || grafanaUri.IsLoopback))
{
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
        "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}")));
}

var failures = new List<string>();
var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

foreach (var file in Directory.EnumerateFiles(dashboardDirectory, "*.json").Order(StringComparer.Ordinal))
{
    using var document = JsonDocument.Parse(await File.ReadAllTextAsync(file));
    var uid = document.RootElement.GetProperty("uid").GetString();
    foreach (var panel in document.RootElement.GetProperty("panels").EnumerateArray())
    {
        var type = panel.GetProperty("type").GetString();
        if (type is "row" or "text")
        {
            continue;
        }

        var title = panel.GetProperty("title").GetString();
        var mode = panel.TryGetProperty("verify", out var verify) ? verify.GetString() : "data";
        foreach (var target in panel.GetProperty("targets").EnumerateArray())
        {
            var datasource = target.TryGetProperty("datasource", out var own) ? own : panel.GetProperty("datasource");
            var query = target.TryGetProperty("expr", out var expr) ? expr : target.GetProperty("query");
            var (ok, series) = await RunTarget(datasource.GetProperty("type").GetString()!, query.GetString()!);
            var passed = ok && (series > 0 || mode == "idle-ok");
            Console.WriteLine($"{(passed ? "ok  " : "FAIL")} {uid}/{title} [{target.GetProperty("refId").GetString()}] series={series} mode={mode}");
            if (!passed)
            {
                failures.Add($"{uid}/{title}");
            }
        }
    }
}

Console.WriteLine(failures.Count == 0 ? "All panel queries verified." : $"\n{failures.Count} failing panel(s)");
return failures.Count == 0 ? 0 : 1;

async Task<(bool Ok, int Series)> RunTarget(string type, string expr)
{
    switch (type)
    {
        case "prometheus":
            var promQuery = expr.Replace("$service", ".*").Replace("$__rate_interval", "5m").Replace("$__interval", "1m");
            using (var response = await Get($"{grafana}/api/datasources/proxy/uid/Prometheus/api/v1/query?query={Uri.EscapeDataString(promQuery)}"))
            {
                return (response.RootElement.GetProperty("status").GetString() == "success",
                    response.RootElement.GetProperty("data").GetProperty("result").GetArrayLength());
            }

        case "loki":
            var lokiQuery = expr.Replace("$__auto", "5m").Replace("$__range", "1h");
            var lokiUrl = $"{grafana}/api/datasources/proxy/uid/Loki/loki/api/v1/query_range?query={Uri.EscapeDataString(lokiQuery)}"
                + $"&start={(now - 3600) * 1_000_000_000}&end={now * 1_000_000_000}&limit=1";
            using (var response = await Get(lokiUrl))
            {
                return (response.RootElement.GetProperty("status").GetString() == "success",
                    response.RootElement.GetProperty("data").GetProperty("result").GetArrayLength());
            }

        case "tempo":
            var tempoUrl = $"{grafana}/api/datasources/proxy/uid/Tempo/api/search?q={Uri.EscapeDataString(expr)}&start={now - 3600}&end={now}&limit=1";
            using (var response = await Get(tempoUrl))
            {
                var failed = response.RootElement.TryGetProperty("status", out var status) && status.GetString() == "error";
                return (!failed, response.RootElement.TryGetProperty("traces", out var traces) ? traces.GetArrayLength() : 0);
            }

        default:
            throw new InvalidOperationException($"Unknown datasource type {type}.");
    }
}

async Task<JsonDocument> Get(string url)
{
    using var response = await http.GetAsync(url);
    var body = await response.Content.ReadAsStringAsync();
    if (response.IsSuccessStatusCode)
    {
        return JsonDocument.Parse(body);
    }

    Console.Error.WriteLine($"HTTP {(int)response.StatusCode} from {new Uri(url).AbsolutePath}: {body[..Math.Min(body.Length, 200)]}");
    return JsonDocument.Parse("""{"status":"error","data":{"result":[]},"traces":[]}""");
}
