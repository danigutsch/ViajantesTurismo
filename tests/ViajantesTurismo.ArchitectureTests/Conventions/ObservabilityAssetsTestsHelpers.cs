using System.Text.Json;
using System.Text.RegularExpressions;
using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Helpers for tests that pin the source-controlled observability assets.</summary>
internal static partial class ObservabilityAssetsTestsHelpers
{
    /// <summary>Dashboard file names the repository must provision.</summary>
    public static readonly string[] ExpectedDashboards =
    [
        "branding-cache", "catalog-pipeline", "collector-gateway", "event-store", "http-platform", "logs",
        "media-accessibility", "platform-smoke", "postgresql-observability", "runtime", "traces"
    ];

    /// <summary>Meter names registered in ServiceDefaults that the catalog must list.</summary>
    public static readonly string[] RegisteredMeters =
    [
        "SharedKernel.Mediator", "ViajantesTurismo.Catalog", "ViajantesTurismo.Branding", "PandaBear.AspNetCore",
        "PandaBear.Blazor", "SharedKernel.EventSourcing.Npgsql", "SharedKernel.Observability.Npgsql"
    ];

    /// <summary>Prefixes of platform metrics that the catalog does not enumerate.</summary>
    public static readonly string[] PlatformMetricPrefixes =
        ["otelcol_", "http_", "kestrel_", "dotnet_", "process_", "aspnetcore_", "db_client", "client_call", "up"];

    /// <summary>Headings every dashboard purpose panel must contain.</summary>
    public static readonly string[] RequiredHeadings = ["Purpose", "Audience", "Decision"];

    /// <summary>Keys every alert rule must carry.</summary>
    public static readonly string[] RequiredRuleKeys = ["summary:", "description:", "runbook:"];

    /// <summary>Alert provisioning files that must exist.</summary>
    public static readonly string[] AlertingFiles = ["rules.yaml", "contact-points.yaml", "policies.yaml", "templates.yaml"];

    /// <summary>Captures meter name constants declared in ServiceDefaults.</summary>
    [GeneratedRegex(@"const string \w+Name = ""([^""]+)""", RegexOptions.CultureInvariant)]
    public static partial Regex MeterNameConstant();

    /// <summary>Strips series, unit, and counter suffixes from a Prometheus metric name.</summary>
    public static string BaseMetricName(string metric)
    {
        var name = SeriesSuffix().Replace(metric, string.Empty);
        name = UnitSuffix().Replace(name, string.Empty);
        return name.EndsWith("_total", StringComparison.Ordinal) ? name[..^"_total".Length] : name;
    }

    /// <summary>Returns the Grafana alert provisioning directory.</summary>
    public static string AlertingRoot() =>
        Path.Combine(GetRepositoryRoot(), "observability", "grafana", "provisioning", "alerting");

    /// <summary>Returns the observability signal catalog path.</summary>
    public static string CatalogPath() => Path.Combine(GetRepositoryRoot(), "docs", "OBSERVABILITY_CATALOG.md");

    /// <summary>Loads every provisioned dashboard keyed by file name.</summary>
    public static Dictionary<string, JsonDocument> LoadDashboards() =>
        Directory.EnumerateFiles(Path.Combine(GetRepositoryRoot(), "observability", "grafana", "dashboards"), "*.json")
            .ToDictionary(path => Path.GetFileNameWithoutExtension(path), path => JsonDocument.Parse(File.ReadAllText(path)));

    /// <summary>Enumerates dashboard panels that are neither rows nor text.</summary>
    public static IEnumerable<(string Dashboard, JsonElement Panel)> DataPanels(Dictionary<string, JsonDocument> dashboards) =>
        dashboards.SelectMany(pair => pair.Value.RootElement.GetProperty("panels").EnumerateArray()
            .Skip(1)
            .Where(panel => panel.GetProperty("type").GetString() is not ("row" or "text"))
            .Select(panel => (pair.Key, panel)));

    /// <summary>Matches grouping by unbounded or sensitive dimensions.</summary>
    [GeneratedRegex(@"by \([^)]*(trace_id|span_id|stream_id|event_id|exception|user|email)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    public static partial Regex ForbiddenGrouping();

    /// <summary>Splits alert rule YAML into one block per rule.</summary>
    [GeneratedRegex(@"\n\s+- uid: ", RegexOptions.CultureInvariant)]
    public static partial Regex RuleSeparator();

    /// <summary>Captures runbook anchors referenced by alert rules.</summary>
    [GeneratedRegex(@"runbook: \S*#(\S+)", RegexOptions.CultureInvariant)]
    public static partial Regex RunbookAnchor();

    /// <summary>Matches a catalog row that ends with a keep, remove, or gap verdict.</summary>
    [GeneratedRegex(@"\| (keep|remove|gap) \|?\s*$", RegexOptions.CultureInvariant)]
    public static partial Regex VerdictCell();

    /// <summary>Matches Prometheus metric names that carry a unit or series suffix.</summary>
    [GeneratedRegex(@"\b[a-z][a-z0-9_]+_(?:total|bucket|seconds|milliseconds)\b", RegexOptions.CultureInvariant)]
    public static partial Regex MetricName();

    /// <summary>Matches histogram series suffixes.</summary>
    [GeneratedRegex(@"_(bucket|count|sum)$", RegexOptions.CultureInvariant)]
    public static partial Regex SeriesSuffix();

    /// <summary>Matches unit suffixes.</summary>
    [GeneratedRegex(@"_(milliseconds|seconds)$", RegexOptions.CultureInvariant)]
    public static partial Regex UnitSuffix();
}
