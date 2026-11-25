using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SharedKernel.Testing.AspNetCore;

namespace ViajantesTurismo.Testing.Hosting;

/// <summary>Inspects the service registrations of an unstarted application host.</summary>
internal sealed class EntryPointHost(IHost host) : IDisposable
{
    /// <summary>Gets the types of the built host's background services.</summary>
    public Type[] HostedServiceTypes => [.. host.Services.GetServices<IHostedService>().Select(service => service.GetType())];

    /// <summary>Returns whether the application registered the specified service.</summary>
    public bool Registers<TService>() =>
        host.Services.GetRequiredService<IServiceProviderIsService>().IsService(typeof(TService));

    /// <summary>Returns whether the application registered a health check with the specified name and tag.</summary>
    public bool HasHealthCheck(string name, string tag) =>
        host.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations
            .Any(check => check.Name == name && check.Tags.Contains(tag));

    /// <summary>Invokes the entry point of <paramref name="assembly" /> with <paramref name="args" /> and returns the host it built.</summary>
    /// <param name="assembly">The application assembly whose entry point builds a host.</param>
    /// <param name="args">The command-line arguments passed to the entry point.</param>
    /// <returns>The built, not started, host. The caller disposes it.</returns>
    public static EntryPointHost Build(Assembly assembly, string[] args) =>
        new(WorkerTestHost.Build(assembly, args));

    /// <inheritdoc />
    public void Dispose() => host.Dispose();
}
