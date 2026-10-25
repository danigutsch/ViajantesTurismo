using System.Diagnostics.Metrics;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Counts document Branding captures whose HTTP result disagrees with the local projection.</summary>
public static class DocumentBrandingMetrics
{
    /// <summary>The Admin meter name.</summary>
    public const string MeterName = "ViajantesTurismo.Admin";

    /// <summary>The projection mismatch counter name.</summary>
    public const string ProjectionMismatchName = "admin.documents.branding.projection_mismatch";

    /// <summary>The tag that names why a capture mismatched.</summary>
    public const string ReasonTagName = "reason";

    /// <summary>The reason recorded when the projection has not applied any revision.</summary>
    public const string ReasonMissing = "missing";

    /// <summary>The reason recorded when the projection holds different Branding values.</summary>
    public const string ReasonDifferent = "different";

    private static Meter Meter { get; } = new(MeterName);

    private static Counter<long> ProjectionMismatches
    {
        get;
    } = Meter.CreateCounter<long>(
        ProjectionMismatchName,
        unit: "{capture}",
        description: "Document Branding captures whose HTTP result differed from the local Admin projection.");

    internal static void RecordProjectionMismatch(string reason) =>
        ProjectionMismatches.Add(1, new KeyValuePair<string, object?>(ReasonTagName, reason));
}
