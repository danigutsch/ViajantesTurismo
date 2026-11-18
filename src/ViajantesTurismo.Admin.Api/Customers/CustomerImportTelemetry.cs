using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ViajantesTurismo.Admin.Api.Customers;

/// <summary>Records the customer import workflow outcome and elapsed time.</summary>
internal sealed class CustomerImportTelemetry
{
    private const string MeterName = "ViajantesTurismo.Admin.CustomerImport";
    private const int KiB = 1024;
    private readonly Meter meter;
    private readonly Counter<long> attempts;
    private readonly Histogram<double> duration;

    /// <summary>Creates instruments scoped to the current host.</summary>
    public CustomerImportTelemetry(IMeterFactory meters)
    {
        meter = meters.Create(MeterName);
        attempts = meter.CreateCounter<long>("admin.customer_import.attempts", "{attempt}");
        duration = meter.CreateHistogram<double>(
            "admin.customer_import.duration",
            "s",
            advice: new()
            {
                HistogramBucketBoundaries = [0.01, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 20, 30, 60, 120],
            });
    }

    /// <summary>Records one attempt and elapsed seconds using only bounded dimensions.</summary>
    internal void Record(Operation operation, Outcome outcome, long length, long start)
    {
        var tags = new TagList
        {
            { "import.operation", operation == Operation.Import ? "import" : "commit" },
            {
                "import.outcome", outcome switch
                {
                    Outcome.Completed => "completed",
                    Outcome.Rejected => "rejected",
                    Outcome.Canceled => "canceled",
                    _ => "failed",
                }
            },
            {
                "import.size_bucket", length switch
                {
                    <= 0 => "unknown",
                    <= 64 * KiB => "up_to_64_kib",
                    <= 256 * KiB => "up_to_256_kib",
                    <= 1024 * KiB => "up_to_1_mib",
                    _ => "over_limit",
                }
            }
        };
        attempts.Add(1, tags);
        duration.Record(Stopwatch.GetElapsedTime(start).TotalSeconds, tags);
    }

    /// <summary>Identifies the route stage.</summary>
    internal enum Operation
    {
        /// <summary>Initial upload and conflict detection.</summary>
        Import,

        /// <summary>Submission with conflict resolutions.</summary>
        Commit,
    }

    /// <summary>The terminal result of an attempt.</summary>
    internal enum Outcome
    {
        /// <summary>Returned a processed import, including partial row success.</summary>
        Completed,

        /// <summary>The upload or all rows were rejected, or conflicts require resolution.</summary>
        Rejected,

        /// <summary>A dependency or the workflow failed.</summary>
        Failed,

        /// <summary>The request token canceled the workflow.</summary>
        Canceled,
    }
}
