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
        ["otelcol_", "http_", "kestrel_", "dotnet_", "process_", "aspnetcore_", "db_client", "client_call"];

    /// <summary>Exact platform metric names that the catalog does not enumerate.</summary>
    public static readonly string[] PlatformMetricNames = ["up"];

    /// <summary>PromQL words that look like metric names but are not.</summary>
    public static readonly string[] PromQlKeywords = ["or", "and", "unless", "offset", "bool", "by", "without", "on", "ignoring", "inf", "nan"];

    /// <summary>Alert rule uids the repository must provision.</summary>
    public static readonly string[] ExpectedAlertRules =
    [
        "branding-invalidation-failures", "catalog-event-errors", "collector-export-failing",
        "collector-refusing-telemetry", "collector-self-telemetry-down", "http-5xx-ratio", "unhandled-exceptions"
    ];

    /// <summary>Datasource uids an alert rule query may use.</summary>
    public static readonly string[] AlertDatasourceUids = ["Prometheus", "Loki", "__expr__"];

    /// <summary>No-data states Grafana accepts for an alert rule.</summary>
    public static readonly string[] NoDataStates = ["OK", "Alerting", "NoData", "KeepLast"];

    /// <summary>Verify modes a dashboard panel may declare.</summary>
    public static readonly string[] VerifyModes = ["data", "idle-ok"];

    /// <summary>Label values each metric can emit, with the source file that emits them.</summary>
    public static readonly (string Metric, string Label, string Source, string[] Values)[] EmittedLabelValues =
    [
        ("branding_cache_invalidations_total", "outcome", "src/ViajantesTurismo.Branding.AspNetCore/BrandingTelemetry.cs", ["succeeded", "failed"]),
        ("branding_cache_requests_total", "outcome", "src/ViajantesTurismo.Branding.AspNetCore/BrandingTelemetry.cs", ["hit", "miss"]),
        ("catalog_integration_event_total", "catalog_outcome", "src/ViajantesTurismo.Catalog.Application/CatalogTelemetry.cs", ["success", "skipped", "acquired", "completed", "error"]),
    ];

    /// <summary>Headings every dashboard purpose panel must contain.</summary>
    public static readonly string[] RequiredHeadings = ["Purpose", "Audience", "Decision"];

    /// <summary>Keys every alert rule must carry.</summary>
    public static readonly string[] RequiredRuleKeys = ["summary:", "description:", "runbook:"];

    /// <summary>Alert provisioning files that must exist.</summary>
    public static readonly string[] AlertingFiles = ["rules.yaml", "contact-points.yaml", "policies.yaml", "templates.yaml"];

    /// <summary>Captures meter name constants declared in ServiceDefaults.</summary>
    [GeneratedRegex(@"const string \w+Name = ""([^""]+)""", RegexOptions.CultureInvariant)]
    public static partial Regex MeterNameConstant();

    /// <summary>Extracts metric names from a PromQL expression, ignoring labels, literals, functions, and variables.</summary>
    public static IEnumerable<string> MetricNames(string expression)
    {
        var stripped = StringLiteral().Replace(expression, string.Empty);
        stripped = GroupingClause().Replace(stripped, " ");
        stripped = SelectorBody().Replace(stripped, string.Empty);
        stripped = GrafanaVariable().Replace(stripped, " ");
        return Identifier().Matches(stripped).Select(match => match.Value).Where(name => !PromQlKeywords.Contains(name));
    }

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

    /// <summary>Matches histogram series suffixes.</summary>
    [GeneratedRegex(@"_(bucket|count|sum)$", RegexOptions.CultureInvariant)]
    public static partial Regex SeriesSuffix();

    /// <summary>Matches unit suffixes.</summary>
    [GeneratedRegex(@"_(milliseconds|seconds)$", RegexOptions.CultureInvariant)]
    public static partial Regex UnitSuffix();

    /// <summary>Enumerates every dashboard query target with its dashboard, panel, datasource type, and expression.</summary>
    public static IEnumerable<(string Dashboard, string Panel, string DatasourceType, string Expression)> DashboardTargets() =>
        LoadDashboards().SelectMany(pair => pair.Value.RootElement.GetProperty("panels").EnumerateArray()
            .Where(panel => panel.TryGetProperty("targets", out _))
            .SelectMany(panel => panel.GetProperty("targets").EnumerateArray().Select(target =>
            {
                var datasource = target.TryGetProperty("datasource", out var own) ? own : panel.GetProperty("datasource");
                var expression = target.TryGetProperty("expr", out var expr) ? expr.GetString()
                    : target.TryGetProperty("query", out var query) ? query.GetString() : null;
                return (pair.Key, panel.GetProperty("title").GetString() ?? string.Empty, datasource.GetProperty("type").GetString() ?? string.Empty, expression ?? string.Empty);
            })));

    /// <summary>Enumerates the alert rule queries from the provisioned rules file with escaped quotes resolved.</summary>
    public static IEnumerable<string> AlertExpressions() =>
        AlertExpressionLine().Matches(File.ReadAllText(Path.Combine(AlertingRoot(), "rules.yaml")))
            .Select(match => match.Groups[1].Value.Replace("\\\"", "\"", StringComparison.Ordinal));

    /// <summary>Matches a Prometheus selector with its metric name and matcher body.</summary>
    [GeneratedRegex(@"(?<metric>[a-z][a-z0-9_]*)\{(?<body>[^}]*)\}", RegexOptions.CultureInvariant)]
    public static partial Regex Selector();

    /// <summary>Matches one label matcher inside a selector body.</summary>
    [GeneratedRegex(@"(?<label>[a-z_][a-z0-9_]*)\s*(?<op>=~|!=|!~|=)\s*""(?<value>[^""]*)""", RegexOptions.CultureInvariant)]
    public static partial Regex LabelMatcher();

    /// <summary>Matches the expression line of an alert rule query.</summary>
    [GeneratedRegex(@"^\s+expr: ""(.*)""\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    public static partial Regex AlertExpressionLine();

    /// <summary>Matches a Loki stream selector and captures its body.</summary>
    [GeneratedRegex(@"\{(?<body>[^}|]*)\}", RegexOptions.CultureInvariant)]
    public static partial Regex StreamSelector();

    /// <summary>Matches a detected_level label filter and captures its pattern.</summary>
    [GeneratedRegex(@"detected_level\s*=~\s*""(?<pattern>[^""]*)""", RegexOptions.CultureInvariant)]
    public static partial Regex LevelFilter();

    /// <summary>Captures the condition ref id of an alert rule.</summary>
    [GeneratedRegex(@"condition:\s*(?<ref>\w+)", RegexOptions.CultureInvariant)]
    public static partial Regex ConditionRef();

    /// <summary>Captures each datasource uid used by an alert rule.</summary>
    [GeneratedRegex(@"datasourceUid:\s*(?<uid>\S+)", RegexOptions.CultureInvariant)]
    public static partial Regex DatasourceUid();

    /// <summary>Captures the no-data state of an alert rule.</summary>
    [GeneratedRegex(@"noDataState:\s*(?<state>\w+)", RegexOptions.CultureInvariant)]
    public static partial Regex NoDataState();

    /// <summary>Captures the name of a defined notification template.</summary>
    [GeneratedRegex(@"define\s+""(?<name>[^""]+)""", RegexOptions.CultureInvariant)]
    public static partial Regex DefinedTemplate();

    /// <summary>Matches a double-quoted string literal.</summary>
    [GeneratedRegex(@"""[^""]*""", RegexOptions.CultureInvariant)]
    public static partial Regex StringLiteral();

    /// <summary>Matches a PromQL grouping or matching clause and its label list.</summary>
    [GeneratedRegex(@"\b(?:by|without|on|ignoring|group_left|group_right)\s*\([^)]*\)", RegexOptions.CultureInvariant)]
    public static partial Regex GroupingClause();

    /// <summary>Matches a label selector body.</summary>
    [GeneratedRegex(@"\{[^}]*\}", RegexOptions.CultureInvariant)]
    public static partial Regex SelectorBody();

    /// <summary>Matches a Grafana dashboard variable.</summary>
    [GeneratedRegex(@"\$\w+", RegexOptions.CultureInvariant)]
    public static partial Regex GrafanaVariable();

    /// <summary>Matches an identifier that is not followed by an opening parenthesis.</summary>
    [GeneratedRegex(@"\b[a-z_][a-z0-9_]*\b(?!\s*\()", RegexOptions.CultureInvariant)]
    public static partial Regex Identifier();
}
