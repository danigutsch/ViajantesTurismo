using System.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using SharedKernel.Scheduling;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Documents;

/// <summary>Owns the lifetime of the application host used for durable maintenance tests.</summary>
internal sealed class DurableDocumentMaintenanceTestHost(IHost host) : IAsyncDisposable
{
    /// <summary>Starts the application's registered maintenance worker.</summary>
    internal Task Start(CancellationToken ct) => host.StartAsync(ct);

    /// <summary>Collects the current batch-age gauge for the requested stable service label.</summary>
    internal static double[] ReadBatchAges(string serviceName)
    {
        using var listener = new MeterListener();
        var durations = new List<double>();
        listener.InstrumentPublished = (instrument, recorder) =>
        {
            if (instrument.Meter.Name == SchedulingTelemetry.Name
                && instrument.Name == SchedulingTelemetry.MetricPollingBatchActiveDuration)
            {
                recorder.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((_, value, tags, _) =>
        {
            foreach (var tag in tags)
            {
                if (tag.Key == SchedulingTelemetry.TagServiceName && Equals(tag.Value, serviceName))
                {
                    durations.Add(value);
                }
            }
        });
        listener.Start();
        listener.RecordObservableInstruments();
        return [.. durations];
    }

    /// <summary>Stops the application's jobs and then disposes the host.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await host.StopAsync(CancellationToken.None);
        }
        finally
        {
            host.Dispose();
        }
    }
}
