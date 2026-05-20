using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
namespace ViajantesTurismo.ServiceDefaults;

internal static class OpenTelemetryBuilderExtensions
{
    public static MeterProviderBuilder AddSharedKernelMediatorMetrics(this MeterProviderBuilder metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        return metrics.AddMeter(global::SharedKernel.Mediator.MediatorTelemetry.Name);
    }

    public static TracerProviderBuilder AddSharedKernelMediatorTracing(this TracerProviderBuilder tracing)
    {
        ArgumentNullException.ThrowIfNull(tracing);

        return tracing.AddSource(global::SharedKernel.Mediator.MediatorTelemetry.Name);
    }
}
