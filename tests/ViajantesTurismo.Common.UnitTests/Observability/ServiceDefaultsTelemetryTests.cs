using System.Diagnostics;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SharedKernel.Mediator;
using ViajantesTurismo.ServiceDefaults;

namespace ViajantesTurismo.Common.UnitTests.Observability;

public sealed class ServiceDefaultsTelemetryTests
{
    [Fact]
    public void Add_Service_Defaults_Exports_Mediator_Custom_Tracing_And_Metrics()
    {
        // Arrange
        var exportedActivities = new ConcurrentQueue<Activity>();
        var exportedMetricNames = new ConcurrentQueue<string>();
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;

        builder.AddServiceDefaults();
        builder.Services.AddSingleton<AppMediatorInstrumentation>();
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.AddProcessor(new SimpleActivityExportProcessor(new CollectingActivityExporter(exportedActivities)));
            })
            .WithMetrics(metrics =>
            {
                metrics.AddReader(new PeriodicExportingMetricReader(new CollectingMetricExporter(exportedMetricNames)));
            });

        using var host = builder.Build();
        var instrumentation = host.Services.GetRequiredService<AppMediatorInstrumentation>();
        var tracerProvider = host.Services.GetRequiredService<TracerProvider>();
        var meterProvider = host.Services.GetRequiredService<MeterProvider>();

        // Act
        using (var activity = SharedKernelMediatorActivitySource.Source.StartActivity("test.dispatch", ActivityKind.Internal))
        {
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        instrumentation.RequestsTotal.Add(
            1,
            new TagList
            {
                { "mediator.request.name", "LookupTour" },
                { "mediator.outcome", "success" }
            });

        tracerProvider.ForceFlush();
        meterProvider.ForceFlush();

        // Assert
        var mediatorActivity = Assert.Single(exportedActivities, activity =>
            string.Equals(activity.Source.Name, SharedKernelMediatorActivitySource.ActivitySourceName, StringComparison.Ordinal));
        Assert.Equal("test.dispatch", mediatorActivity.OperationName);
        Assert.Contains("mediator.requests", exportedMetricNames, StringComparer.Ordinal);
    }

    private sealed class CollectingActivityExporter(ConcurrentQueue<Activity> exportedActivities) : BaseExporter<Activity>
    {
        public override ExportResult Export(in Batch<Activity> batch)
        {
            foreach (var activity in batch)
            {
                exportedActivities.Enqueue(activity);
            }

            return ExportResult.Success;
        }
    }

    private sealed class CollectingMetricExporter(ConcurrentQueue<string> exportedMetricNames) : BaseExporter<Metric>
    {
        public override ExportResult Export(in Batch<Metric> batch)
        {
            foreach (var metric in batch)
            {
                exportedMetricNames.Enqueue(metric.Name);
            }

            return ExportResult.Success;
        }
    }
}
