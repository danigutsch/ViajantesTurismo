using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using PandaBear.IO;

namespace ViajantesTurismo.Testing.Reproduction;

/// <summary>Reads and writes bounded manifests using the fixture's reviewed values.</summary>
internal static class ReproductionManifestCodec
{
    private const int SchemaVersion = 1;
    private const int MaximumBytes = 64 * 1024;
    private const int MaximumDurationMs = 10 * 60 * 1000;
    private const string ExportSetting = "VIAJANTESTURISMO_REPRODUCTION_MANIFESTS";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        AllowDuplicateProperties = false,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        MaxDepth = 4,
        WriteIndented = true
    };

    /// <summary>Reads the source revision recorded by the fixture's build.</summary>
    internal static string GetSourceRevision(Assembly fixtureAssembly)
    {
        ArgumentNullException.ThrowIfNull(fixtureAssembly);
        var version = fixtureAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var delimiter = version?.LastIndexOfAny(['+', '.']) ?? -1;
        var revision = delimiter < 0 ? null : version?[(delimiter + 1)..];
        return IsRevision(revision)
            ? revision
            : throw new InvalidOperationException("Reproduction requires a test assembly built from a Git checkout.");
    }

    /// <summary>Resolves the opt-in local test results directory from the fixture build's checkout.</summary>
    internal static string? GetLocalResultsDirectory()
    {
        if (Environment.GetEnvironmentVariable(ExportSetting) != "1")
        {
            return null;
        }

        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ViajantesTurismo.slnx")))
            {
                return RepositoryPath.Parse("TestResults/reproduction").ResolveUnder(directory.FullName);
            }
        }

        throw new InvalidOperationException("Reproduction exports require the repository's local TestResults directory.");
    }

    /// <summary>Validates the attempt and creates a new manifest named with its opaque attempt ID.</summary>
    internal static async Task<string> Write(
        string resultsDirectory, ReproductionSpecification specification, ReproductionManifest manifest, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(manifest);
        ct.ThrowIfCancellationRequested();

        var payload = new ReproductionManifestPayload(
            SchemaVersion, manifest.Revision, manifest.Scenario, manifest.AttemptId,
            [.. manifest.TraceIds.Select(static trace => trace.ToHexString())],
            manifest.Outcome.Result, manifest.Outcome.OutcomeCode, new Dictionary<string, string>(manifest.Outcome.ResourceStates, StringComparer.Ordinal),
            manifest.StartedUtc, manifest.DurationMs, manifest.Seed);
        _ = Validate(payload, specification);
        var json = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        if (json.Length > MaximumBytes)
        {
            throw InvalidManifest();
        }

        Directory.CreateDirectory(resultsDirectory);
        var destination = RepositoryPath.Parse($"{payload.AttemptId:N}.json").ResolveUnder(resultsDirectory);
        var temporary = RepositoryPath.Parse($".{Guid.NewGuid():N}.tmp").ResolveUnder(resultsDirectory);
        var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
        try
        {
            await using (file.ConfigureAwait(false))
            {
                await file.WriteAsync(json, ct).ConfigureAwait(false);
            }

            ct.ThrowIfCancellationRequested();
            File.Move(temporary, destination);
        }
        finally
        {
            File.Delete(temporary);
        }

        return destination;
    }

    /// <summary>Reads one manifest and refuses anything outside the fixture's schema and value allowlist.</summary>
    internal static async Task<ReproductionManifest> Read(string path, ReproductionSpecification specification, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(specification);
        var bytes = await BoundedReader.ReadFile(path, MaximumBytes, ct).ConfigureAwait(false) ?? throw InvalidManifest();
        ReproductionManifestPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ReproductionManifestPayload>(bytes, JsonOptions);
        }
        catch (JsonException)
        {
            throw InvalidManifest();
        }

        return Validate(payload ?? throw InvalidManifest(), specification);
    }

    private static ReproductionManifest Validate(ReproductionManifestPayload payload, ReproductionSpecification specification)
    {
        if (payload.SchemaVersion != SchemaVersion || !IsRevision(payload.Revision) || payload.Revision != specification.Revision
            || payload.Scenario != specification.Scenario || payload.Seed != specification.Seed || payload.AttemptId == Guid.Empty
            || payload.StartedUtc == default || payload.StartedUtc.Offset != TimeSpan.Zero
            || payload.DurationMs is < 0 or > MaximumDurationMs)
        {
            throw InvalidManifest();
        }

        var allowed = specification.AllowedOutcomes.Any(outcome => outcome.Result == payload.Result && outcome.OutcomeCode == payload.OutcomeCode
            && outcome.ResourceStates.Count == payload.ResourceStates.Count
            && outcome.ResourceStates.All(state => payload.ResourceStates.TryGetValue(state.Key, out var observed) && observed == state.Value));
        if (!allowed)
        {
            throw InvalidManifest();
        }

        var traces = payload.TraceIds.Select(ParseTraceId).ToArray();
        if (traces.Distinct().Count() != traces.Length)
        {
            throw InvalidManifest();
        }

        return new ReproductionManifest(
            payload.Revision, payload.Scenario, payload.AttemptId, traces,
            new ReproductionOutcome(payload.Result, payload.OutcomeCode, payload.ResourceStates), payload.StartedUtc, payload.DurationMs, payload.Seed);
    }

    private static ActivityTraceId ParseTraceId(string? value)
    {
        if (value is not { Length: 32 } || !IsNonzeroLowerHex(value))
        {
            throw InvalidManifest();
        }

        return ActivityTraceId.CreateFromString(value);
    }

    private static bool IsRevision([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] string? value) =>
        value is { Length: 40 or 64 } && IsNonzeroLowerHex(value);

    private static bool IsNonzeroLowerHex(string value) =>
        value.All(static character => character is >= '0' and <= '9' or >= 'a' and <= 'f')
        && value.Any(static character => character != '0');

    private static InvalidDataException InvalidManifest() => new("The reproduction manifest does not match the approved fixture.");
}
