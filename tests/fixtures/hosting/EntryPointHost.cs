using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ViajantesTurismo.Testing.Hosting;

/// <summary>Runs an application entry point until it builds its host, then stops it before the host starts.</summary>
/// <remarks>
/// The hosting library reports every built host through the <c>Microsoft.Extensions.Hosting</c> diagnostic listener.
/// Only the host built on the calling execution context is captured, so hosts built by parallel tests are left alone.
/// </remarks>
internal sealed class EntryPointHost(IHost host) : IDisposable
{
    private static readonly AsyncLocal<StrongBox<IHost?>?> CurrentCapture = new();

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
    public static EntryPointHost Build(Assembly assembly, string[] args)
    {
        var entryPoint = assembly.EntryPoint
            ?? throw new InvalidOperationException($"{assembly.GetName().Name} has no entry point.");
        var capture = new StrongBox<IHost?>();
        using var observer = new HostBuiltObserver(CaptureHost);
        using var subscription = DiagnosticListener.AllListeners.Subscribe(observer);

        CurrentCapture.Value = capture;
        try
        {
            entryPoint.Invoke(null, [args]);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is HostAbortedException)
        {
        }
        finally
        {
            CurrentCapture.Value = null;
        }

        var builtHost = capture.Value
            ?? throw new InvalidOperationException($"{assembly.GetName().Name} returned without building a host.");
        return new EntryPointHost(builtHost);
    }

    /// <inheritdoc />
    public void Dispose() => host.Dispose();

    private static void CaptureHost(IHost host)
    {
        var capture = CurrentCapture.Value;
        if (capture is null)
        {
            return;
        }

        capture.Value = host;
        throw new HostAbortedException();
    }
}
