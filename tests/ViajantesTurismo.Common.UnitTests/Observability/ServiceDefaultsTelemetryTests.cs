using System.Diagnostics;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
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
    private const string PostgreSqlEventSourcingTelemetryName = "SharedKernel.EventSourcing.PostgreSQL";
    private const string PostgreSqlEventSourcingActivityAppend = "eventsourcing.postgresql.append";
    private const string PostgreSqlEventSourcingMetricEventsAppended = "eventsourcing.postgresql.event.appended";
    private const string CatalogTelemetryName = "ViajantesTurismo.Catalog";
    private const string CatalogActivityIntegrationEventHandle = "catalog.integration_event.handle";
    private const string CatalogMetricIntegrationEvent = "catalog.integration_event";

    [Fact]
    public void Add_service_defaults_exports_catalog_custom_tracing_and_metrics()
    {
        // Arrange
        var exportedActivities = new ConcurrentQueue<Activity>();
        var exportedMetricNames = new ConcurrentQueue<string>();
        using var activitySource = new ActivitySource(CatalogTelemetryName);
        using var meter = new Meter(CatalogTelemetryName);
        var counter = meter.CreateCounter<long>(CatalogMetricIntegrationEvent);
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;

        builder.AddServiceDefaults();
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
        var tracerProvider = host.Services.GetRequiredService<TracerProvider>();
        var meterProvider = host.Services.GetRequiredService<MeterProvider>();

        // Act
        using (var activity = activitySource.StartActivity(CatalogActivityIntegrationEventHandle, ActivityKind.Consumer))
        {
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        counter.Add(1);
        tracerProvider.ForceFlush();
        meterProvider.ForceFlush();

        // Assert
        var catalogActivity = Assert.Single(exportedActivities, activity =>
            string.Equals(activity.Source.Name, CatalogTelemetryName, StringComparison.Ordinal));
        Assert.Equal(CatalogActivityIntegrationEventHandle, catalogActivity.OperationName);
        Assert.Contains(CatalogMetricIntegrationEvent, exportedMetricNames, StringComparer.Ordinal);
    }

    [Fact]
    public void Add_service_defaults_exports_mediator_custom_tracing_and_metrics()
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

    [Fact]
    public void Add_service_defaults_exports_sharedkernel_provider_tracing_and_metrics()
    {
        // Arrange
        var exportedActivities = new ConcurrentQueue<Activity>();
        var exportedMetricNames = new ConcurrentQueue<string>();
        using var activitySource = new ActivitySource(PostgreSqlEventSourcingTelemetryName);
        using var meter = new Meter(PostgreSqlEventSourcingTelemetryName);
        var counter = meter.CreateCounter<long>(PostgreSqlEventSourcingMetricEventsAppended);
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;

        builder.AddServiceDefaults();
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
        var tracerProvider = host.Services.GetRequiredService<TracerProvider>();
        var meterProvider = host.Services.GetRequiredService<MeterProvider>();

        // Act
        using (var activity = activitySource.StartActivity(PostgreSqlEventSourcingActivityAppend, ActivityKind.Internal))
        {
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        counter.Add(1);
        tracerProvider.ForceFlush();
        meterProvider.ForceFlush();

        // Assert
        var providerActivity = Assert.Single(exportedActivities, activity =>
            string.Equals(activity.Source.Name, PostgreSqlEventSourcingTelemetryName, StringComparison.Ordinal));
        Assert.Equal(PostgreSqlEventSourcingActivityAppend, providerActivity.OperationName);
        Assert.Contains(PostgreSqlEventSourcingMetricEventsAppended, exportedMetricNames, StringComparer.Ordinal);
    }

}
