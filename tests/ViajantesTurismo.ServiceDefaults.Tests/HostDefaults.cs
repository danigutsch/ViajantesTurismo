using Microsoft.Extensions.Hosting;
using PandaBear.ServiceDefaults;

namespace ViajantesTurismo.ServiceDefaults.Tests;

/// <summary>Adds the service defaults the way every ViajantesTurismo host does.</summary>
internal static class HostDefaults
{
    /// <summary>Adds the SharedKernel service defaults and the ViajantesTurismo telemetry on top.</summary>
    public static void Add<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.AddSharedKernelServiceDefaults(ViajantesTurismoServiceDefaults.Configure)
            .AddViajantesTurismoTelemetry();
    }
}
