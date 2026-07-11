using System.Diagnostics;
using System.Diagnostics.Metrics;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal static class ClamAvMediaUploadScannerTelemetry
{
    public const string Name = "ViajantesTurismo.Catalog.MediaUpload";
    public const string ActivityScan = "malware_scan.clamav.scan";

    private static ActivitySource ActivitySource { get; } = new(Name);
    private static Meter Meter { get; } = new(Name);
    private static Counter<long> Scans { get; } = Meter.CreateCounter<long>("malware_scan.requests", unit: "{scan}");
    private static Histogram<double> Duration { get; } = Meter.CreateHistogram<double>("malware_scan.duration", unit: "s");
    private static Histogram<long> Bytes { get; } = Meter.CreateHistogram<long>("malware_scan.bytes", unit: "By");

    public static Activity? StartScan() => ActivitySource.StartActivity(ActivityScan, ActivityKind.Client);

    public static void Record(Activity? activity, MediaUploadScanStatus status, long length, TimeSpan duration, string? errorType = null)
    {
        var outcome = status switch
        {
            MediaUploadScanStatus.Passed => "clean",
            MediaUploadScanStatus.Rejected => "infected",
            _ => "error"
        };
        var tags = new TagList
        {
            { "malware_scan.engine", "clamav" },
            { "malware_scan.transport", "tcp" },
            { "malware_scan.mode", "instream" },
            { "malware_scan.outcome", outcome }
        };
        if (errorType is not null)
        {
            tags.Add("error.type", errorType);
            activity?.SetStatus(ActivityStatusCode.Error);
        }

        activity?.SetTag("malware_scan.engine", "clamav");
        activity?.SetTag("malware_scan.transport", "tcp");
        activity?.SetTag("malware_scan.mode", "instream");
        activity?.SetTag("malware_scan.outcome", outcome);
        activity?.SetTag("error.type", errorType);
        Scans.Add(1, tags);
        Duration.Record(duration.TotalSeconds, tags);
        Bytes.Record(length, tags);
    }
}
