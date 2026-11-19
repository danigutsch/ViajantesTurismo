using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using ViajantesTurismo.Catalog.Application;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Records only Catalog upload measurements emitted by one test's intake call.</summary>
internal sealed class MediaImageUploadMetricsTestSupport : IDisposable
{
    private const string Attempts = "catalog.media_upload.attempts";

    private const string StageDuration = "catalog.media_upload.stage.duration";

    private readonly Activity operation = new Activity(nameof(MediaImageUploadMetricsTestSupport)).Start();

    private readonly MeterListener listener = new();

    private readonly ConcurrentQueue<(string Name, string? Unit, double Value, KeyValuePair<string, object?>[] Tags)> measurements = new();

    private IReadOnlyList<double>? stageBoundaries;

    /// <summary>Starts the listener for the upload instruments in the current logical operation.</summary>
    public MediaImageUploadMetricsTestSupport()
    {
        listener.InstrumentPublished = (instrument, owner) =>
        {
            if (instrument.Meter.Name == CatalogTelemetry.Name && instrument.Name is Attempts or StageDuration)
            {
                if (instrument is Histogram<double> histogram && instrument.Name == StageDuration)
                {
                    stageBoundaries = histogram.Advice?.HistogramBucketBoundaries;
                }

                owner.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Record(instrument, value, tags));
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Record(instrument, value, tags));
        listener.Start();
    }

    /// <summary>Builds a request with synthetic sensitive fields that must never become metric tags.</summary>
    public static MediaImageUploadIntakeRequest CreateRequest(Stream content, int length) => new(
        Guid.CreateVersion7(),
        content,
        "private-traveler-photo.jpg",
        "image/jpeg",
        length,
        "Synthetic private description",
        [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

    /// <summary>Verifies the whole bounded attribute set, stage order, and measurement units.</summary>
    public void AssertMeasurements(string expectedOutcome, string[] expectedStages)
    {
        var (_, countUnit, count, countTags) = measurements.Where(measurement => measurement.Name == Attempts).ShouldHaveSingleItem();
        countUnit.ShouldBe("{attempt}");
        count.ShouldBe(1);
        countTags.ShouldBe([new KeyValuePair<string, object?>("upload.outcome", expectedOutcome)]);
        var durations = measurements.Where(measurement => measurement.Name == StageDuration).ToArray();
        durations.Length.ShouldBe(expectedStages.Length);
        if (durations.Length > 0)
        {
            stageBoundaries.ShouldBe([0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 15, 20, 30, 60]);
        }

        for (var index = 0; index < durations.Length; index++)
        {
            var (_, unit, duration, tags) = durations[index];
            unit.ShouldBe("s");
            duration.ShouldBeGreaterThanOrEqualTo(0);
            tags.ShouldBe([new KeyValuePair<string, object?>("upload.stage", expectedStages[index])]);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        listener.Dispose();
        operation.Dispose();
    }

    private void Record(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (ReferenceEquals(Activity.Current, operation))
        {
            measurements.Enqueue((instrument.Name, instrument.Unit, value, tags.ToArray()));
        }
    }
}
