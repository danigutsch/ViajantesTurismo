using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Hosting;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Runs an application entry point until it builds its host, then stops it before the host starts.</summary>
/// <remarks>
/// The hosting library reports every built host through the <c>Microsoft.Extensions.Hosting</c> diagnostic listener.
/// Only the host built on the calling execution context is captured, so hosts built by parallel tests are left alone.
/// </remarks>
internal static class EntryPointHost
{
    private static readonly AsyncLocal<StrongBox<IHost?>?> CurrentCapture = new();

    /// <summary>Invokes the entry point of <paramref name="assembly" /> with <paramref name="args" /> and returns the host it built.</summary>
    /// <param name="assembly">The application assembly whose entry point builds a host.</param>
    /// <param name="args">The command-line arguments passed to the entry point.</param>
    /// <returns>The built, not started, host. The caller disposes it.</returns>
    public static IHost Build(Assembly assembly, string[] args)
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

        return capture.Value
            ?? throw new InvalidOperationException($"{assembly.GetName().Name} returned without building a host.");
    }

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
