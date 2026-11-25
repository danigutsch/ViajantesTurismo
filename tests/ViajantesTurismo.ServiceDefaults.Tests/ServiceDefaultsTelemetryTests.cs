using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace ViajantesTurismo.ServiceDefaults.Tests;

[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ServiceDefaultsArea)]
public sealed class ServiceDefaultsTelemetryTests
{
    [Theory]
    [InlineData("ViajantesTurismo.Admin.Contracts.Clients")]
    [InlineData("ViajantesTurismo.Catalog")]
    [InlineData("ViajantesTurismo.MigrationService.DatabaseInitializationWorker")]
    public void Host_defaults_export_every_application_activity_source(string sourceName)
    {
        // Arrange
        var exportedActivities = new ConcurrentQueue<Activity>();
        using var activitySource = new ActivitySource(sourceName);
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ApplicationName = "ViajantesTurismo.Management.Web" });
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;
        HostDefaults.Add(builder);
        builder.Services.AddOpenTelemetry().WithTracing(tracing =>
            tracing.AddProcessor(new SimpleActivityExportProcessor(new CollectingActivityExporter(exportedActivities))));
        using var host = builder.Build();
        var tracerProvider = host.Services.GetRequiredService<TracerProvider>();

        // Act
        using (var activity = activitySource.StartActivity("operation", ActivityKind.Client))
        {
            activity?.SetStatus(ActivityStatusCode.Error);
        }

        tracerProvider.ForceFlush();

        // Assert
        var exported = exportedActivities.ShouldHaveSingleItem(activity => string.Equals(activity.Source.Name, sourceName, StringComparison.Ordinal));
        exported.OperationName.ShouldBe("operation");
        exported.Status.ShouldBe(ActivityStatusCode.Error);
    }

    [Theory]
    [InlineData("ViajantesTurismo.Catalog", "catalog.integration_event")]
    [InlineData("ViajantesTurismo.Branding", "branding.revision")]
    [InlineData("Npgsql", "db.client.connection.count")]
    [InlineData("SharedKernel.AspNetCore.Authentication.Bff", "sharedkernel.bff.session_ticket.failures")]
    public void Host_defaults_export_registered_meters(string meterName, string instrumentName)
    {
        // Arrange
        var exportedMetricNames = new ConcurrentQueue<string>();
        using var meter = new Meter(meterName);
        var counter = meter.CreateCounter<long>(instrumentName);
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;
        HostDefaults.Add(builder);
        builder.Services.AddOpenTelemetry().WithMetrics(metrics =>
            metrics.AddReader(new PeriodicExportingMetricReader(new CollectingMetricExporter(exportedMetricNames))));
        using var host = builder.Build();
        var meterProvider = host.Services.GetRequiredService<MeterProvider>();

        // Act
        counter.Add(1);
        meterProvider.ForceFlush();

        // Assert
        exportedMetricNames.ShouldContain(instrumentName, StringComparer.Ordinal);
    }

    [Fact]
    public void Host_defaults_do_not_export_unrelated_meters()
    {
        // Arrange
        var exportedMetricNames = new ConcurrentQueue<string>();
        using var meter = new Meter("Unrelated.Library");
        var counter = meter.CreateCounter<long>("unrelated.count");
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;
        HostDefaults.Add(builder);
        builder.Services.AddOpenTelemetry().WithMetrics(metrics =>
            metrics.AddReader(new PeriodicExportingMetricReader(new CollectingMetricExporter(exportedMetricNames))));
        using var host = builder.Build();
        var meterProvider = host.Services.GetRequiredService<MeterProvider>();

        // Act
        counter.Add(1);
        meterProvider.ForceFlush();

        // Assert
        exportedMetricNames.ShouldNotContain("unrelated.count", StringComparer.Ordinal);
    }
}
