using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
namespace ViajantesTurismo.ServiceDefaults;

internal static class OpenTelemetryBuilderExtensions
{
    private const string CatalogTelemetryName = "ViajantesTurismo.Catalog";
    private const string SharedKernelPostgreSqlEventSourcingName = "SharedKernel.EventSourcing.PostgreSQL";

    public static MeterProviderBuilder AddSharedKernelMediatorMetrics(this MeterProviderBuilder metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        return metrics.AddMeter(SharedKernel.Mediator.MediatorTelemetry.Name);
    }

    public static TracerProviderBuilder AddSharedKernelMediatorTracing(this TracerProviderBuilder tracing)
    {
        ArgumentNullException.ThrowIfNull(tracing);

        return tracing.AddSource(SharedKernel.Mediator.MediatorTelemetry.Name);
    }

    public static MeterProviderBuilder AddSharedKernelProviderMetrics(this MeterProviderBuilder metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        return metrics.AddMeter(SharedKernelPostgreSqlEventSourcingName);
    }

    public static TracerProviderBuilder AddSharedKernelProviderTracing(this TracerProviderBuilder tracing)
    {
        ArgumentNullException.ThrowIfNull(tracing);

        return tracing.AddSource(SharedKernelPostgreSqlEventSourcingName);
    }

    public static MeterProviderBuilder AddCatalogMetrics(this MeterProviderBuilder metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        return metrics.AddMeter(CatalogTelemetryName);
    }

    public static TracerProviderBuilder AddCatalogTracing(this TracerProviderBuilder tracing)
    {
        ArgumentNullException.ThrowIfNull(tracing);

        return tracing.AddSource(CatalogTelemetryName);
    }
}
