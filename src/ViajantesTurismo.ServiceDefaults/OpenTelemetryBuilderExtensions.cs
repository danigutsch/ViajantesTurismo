using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
namespace ViajantesTurismo.ServiceDefaults;

internal static class OpenTelemetryBuilderExtensions
{
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
}
