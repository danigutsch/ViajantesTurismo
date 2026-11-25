using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SharedKernel.ServiceDefaults;

namespace ViajantesTurismo.ServiceDefaults.Tests;

/// <summary>Configures a host with the application defaults and exposes its typed test seams.</summary>
internal sealed class HostDefaults : IDisposable
{
    private readonly IHost _host;

    private HostDefaults(IHost host)
    {
        _host = host;
    }

    /// <summary>Gets the configured service defaults options.</summary>
    public ServiceDefaultsOptions Options => _host.Services.GetRequiredService<ServiceDefaultsOptions>();

    /// <summary>Gets the tracer provider for flushing recorded activities.</summary>
    public TracerProvider TracerProvider => _host.Services.GetRequiredService<TracerProvider>();

    /// <summary>Gets the meter provider for flushing recorded measurements.</summary>
    public MeterProvider MeterProvider => _host.Services.GetRequiredService<MeterProvider>();

    /// <summary>Gets the services registered to run with the host.</summary>
    public IEnumerable<IHostedService> HostedServices => _host.Services.GetServices<IHostedService>();

    /// <summary>Adds the SharedKernel service defaults and the ViajantesTurismo telemetry on top.</summary>
    public static void Add<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.AddApplicationDefaults(ViajantesTurismoServiceDefaults.Configure)
            .AddViajantesTurismoTelemetry();
    }

    /// <summary>Builds and owns the configured test host.</summary>
    public static HostDefaults Build(HostApplicationBuilder builder) => new(builder.Build());

    /// <summary>Gets the logger for the category exercised by a privacy test.</summary>
    /// <typeparam name="TCategory">The category that identifies the expected log records.</typeparam>
    public ILogger<TCategory> Logger<TCategory>() => _host.Services.GetRequiredService<ILogger<TCategory>>();

    /// <inheritdoc />
    public void Dispose() => _host.Dispose();
}
