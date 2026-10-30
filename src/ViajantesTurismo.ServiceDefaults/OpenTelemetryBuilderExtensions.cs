using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
namespace ViajantesTurismo.ServiceDefaults;

/// <summary>OpenTelemetry registration helpers for SharedKernel, Catalog, and Branding telemetry.</summary>
internal static class OpenTelemetryBuilderExtensions
{
    private const string CatalogTelemetryName = "ViajantesTurismo.Catalog";
    private const string BrandingTelemetryName = "ViajantesTurismo.Branding";
    private const string SharedKernelAspNetCoreName = "PandaBear.AspNetCore";
    private const string SharedKernelBlazorName = "PandaBear.Blazor";
    private const string SharedKernelNpgsqlEventSourcingName = "SharedKernel.EventSourcing.Npgsql";

    /// <summary>Adds the SharedKernel event-sourcing provider meter.</summary>
    public static MeterProviderBuilder AddSharedKernelProviderMetrics(this MeterProviderBuilder metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        return metrics.AddMeter(SharedKernelNpgsqlEventSourcingName);
    }

    /// <summary>Adds the SharedKernel event-sourcing provider activity source.</summary>
    public static TracerProviderBuilder AddSharedKernelProviderTracing(this TracerProviderBuilder tracing)
    {
        ArgumentNullException.ThrowIfNull(tracing);

        return tracing.AddSource(SharedKernelNpgsqlEventSourcingName);
    }

    /// <summary>Adds the SharedKernel ASP.NET Core error and security meter (<c>PandaBear.AspNetCore</c>).</summary>
    public static MeterProviderBuilder AddSharedKernelAspNetCoreMetrics(this MeterProviderBuilder metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        return metrics.AddMeter(SharedKernelAspNetCoreName);
    }

    /// <summary>Adds the SharedKernel Blazor handled UI failure meter (<c>PandaBear.Blazor</c>).</summary>
    public static MeterProviderBuilder AddSharedKernelBlazorMetrics(this MeterProviderBuilder metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        return metrics.AddMeter(SharedKernelBlazorName);
    }

    /// <summary>Adds the Catalog meter.</summary>
    public static MeterProviderBuilder AddCatalogMetrics(this MeterProviderBuilder metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        return metrics.AddMeter(CatalogTelemetryName);
    }

    /// <summary>Adds the Branding meter.</summary>
    public static MeterProviderBuilder AddBrandingMetrics(this MeterProviderBuilder metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        return metrics.AddMeter(BrandingTelemetryName);
    }

    /// <summary>Adds the Catalog activity source.</summary>
    public static TracerProviderBuilder AddCatalogTracing(this TracerProviderBuilder tracing)
    {
        ArgumentNullException.ThrowIfNull(tracing);

        return tracing.AddSource(CatalogTelemetryName);
    }
}
