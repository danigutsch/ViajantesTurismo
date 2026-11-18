using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>Records Catalog intake outcomes and stage latency with a fixed set of labels.</summary>
internal static class MediaImageUploadMetrics
{
    private static readonly Counter<long> Attempts = CatalogTelemetry.Meter.CreateCounter<long>(
        "catalog.media_upload.attempts", "{attempt}", "Completed media intake decisions and failed object writes.");

    private static readonly Histogram<double> StageDuration = CatalogTelemetry.Meter.CreateHistogram(
        "catalog.media_upload.stage.duration",
        "s",
        "Duration of Catalog media intake scan and store calls.",
        advice: new InstrumentAdvice<double>
        {
            HistogramBucketBoundaries = [0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 15, 20, 30, 60],
        });

    /// <summary>Terminal intake outcomes that are exposed as a counter dimension.</summary>
    internal enum Outcome
    {
        /// <summary>The image and its storage event were accepted.</summary>
        Accepted,

        /// <summary>File or upload metadata validation failed.</summary>
        InvalidFile,

        /// <summary>The scan did not approve the file.</summary>
        ScanRejected,

        /// <summary>The scanner could not produce a decision.</summary>
        ScannerUnavailable,

        /// <summary>A concurrent gallery edit prevented the image from being saved.</summary>
        GalleryConflict,

        /// <summary>The original object write failed.</summary>
        StorageFailed,
    }

    /// <summary>External intake operations that are timed independently.</summary>
    internal enum Stage
    {
        /// <summary>Malware scan.</summary>
        Scan,

        /// <summary>Original object write.</summary>
        Store,
    }

    /// <summary>Counts one terminal result using the intake's bounded outcome.</summary>
    public static void RecordAttempt(Outcome outcome) => Attempts.Add(1, new KeyValuePair<string, object?>("upload.outcome", outcome switch
    {
        Outcome.Accepted => "accepted",
        Outcome.InvalidFile => "invalid_file",
        Outcome.ScanRejected => "scan_rejected",
        Outcome.ScannerUnavailable => "scanner_unavailable",
        Outcome.GalleryConflict => "gallery_conflict",
        Outcome.StorageFailed => "storage_failed",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
    }));

    /// <summary>Records elapsed seconds for an attempted scan or object-store write.</summary>
    public static void RecordStage(Stage stage, long started) =>
        StageDuration.Record(Stopwatch.GetElapsedTime(started).TotalSeconds, new KeyValuePair<string, object?>("upload.stage", stage switch
        {
            Stage.Scan => "scan",
            Stage.Store => "store",
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null),
        }));
}
