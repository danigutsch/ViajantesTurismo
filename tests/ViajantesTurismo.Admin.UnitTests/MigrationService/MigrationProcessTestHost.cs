using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using PandaBear.Testing.AspNetCore;
using ViajantesTurismo.MigrationService;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

/// <summary>
/// Test host that runs the migration worker and records lifecycle calls, tokens and optional simulated failures.
/// </summary>
internal sealed class MigrationProcessTestHost : IHost
{
    private readonly bool cancelDuringInitialization;
    private readonly ActivitySource activitySource = new(DatabaseInitializationWorker.ActivitySourceName);
    private readonly CancellationToken applicationStopping;
    private readonly Exception? disposeFailure;
    private readonly TestHostApplicationLifetime lifetime = new();
    private readonly ServiceProvider provider;
    private readonly Exception? startFailure;
    private readonly Exception? stopFailure;
    private readonly List<string> lifecycleEvents = [];

    /// <summary>
    /// Creates a host whose worker runs <paramref name="initializationOperation"/>, optionally cancelling during initialization or failing on start, stop or dispose.
    /// </summary>
    public MigrationProcessTestHost(
        Func<CancellationToken, Task> initializationOperation,
        bool cancelDuringInitialization = false,
        Exception? startFailure = null,
        Exception? stopFailure = null,
        Exception? disposeFailure = null)
    {
        ArgumentNullException.ThrowIfNull(initializationOperation);

        this.cancelDuringInitialization = cancelDuringInitialization;
        this.startFailure = startFailure;
        this.stopFailure = stopFailure;
        this.disposeFailure = disposeFailure;
        applicationStopping = lifetime.ApplicationStopping;

        var services = new ServiceCollection();
        services.AddSingleton<IHostApplicationLifetime>(lifetime);
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment("ViajantesTurismo.MigrationService.Tests")
        {
            EnvironmentName = Environments.Production,
        });
        services.AddSingleton(serviceProvider => new DatabaseInitializationWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            serviceProvider.GetRequiredService<IHostEnvironment>(),
            NullLogger<DatabaseInitializationWorker>.Instance,
            (_, ct) => RunInitialization(initializationOperation, ct),
            static (_, _) => Task.CompletedTask,
            activitySource));
        provider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Gets the host service provider.
    /// </summary>
    public IServiceProvider Services => provider;

    /// <summary>
    /// Gets the token cancelled when the application is asked to stop.
    /// </summary>
    public CancellationToken ApplicationStopping => applicationStopping;

    /// <summary>
    /// Gets whether <see cref="Dispose"/> was called.
    /// </summary>
    public bool DisposeCalled
    {
        get; private set;
    }

    /// <summary>
    /// Gets whether the initialization operation ran.
    /// </summary>
    public bool InitializationCalled
    {
        get; private set;
    }

    /// <summary>
    /// Gets the token passed to the initialization operation.
    /// </summary>
    public CancellationToken InitializationToken
    {
        get; private set;
    }

    /// <summary>
    /// Gets whether <see cref="StartAsync"/> was called.
    /// </summary>
    public bool StartCalled
    {
        get; private set;
    }

    /// <summary>
    /// Gets whether <see cref="StopAsync"/> was called.
    /// </summary>
    public bool StopCalled
    {
        get; private set;
    }

    /// <summary>
    /// Gets the ordered lifecycle events recorded (Start, Initialize, Stop, Dispose).
    /// </summary>
    public IReadOnlyList<string> LifecycleEvents => lifecycleEvents;

    /// <summary>
    /// Gets the token passed to <see cref="StartAsync"/>.
    /// </summary>
    public CancellationToken StartToken
    {
        get; private set;
    }

    /// <summary>
    /// Gets the token passed to <see cref="StopAsync"/>.
    /// </summary>
    public CancellationToken StopToken
    {
        get; private set;
    }

    /// <summary>
    /// Records the start and completes, or faults with the configured start failure.
    /// </summary>
    public Task StartAsync(CancellationToken ct)
    {
        StartCalled = true;
        StartToken = ct;
        lifecycleEvents.Add("Start");

        return startFailure is null ? Task.CompletedTask : Task.FromException(startFailure);
    }

    /// <summary>
    /// Records the stop and completes, or faults with the configured stop failure.
    /// </summary>
    public Task StopAsync(CancellationToken ct)
    {
        StopCalled = true;
        StopToken = ct;
        lifecycleEvents.Add("Stop");

        return stopFailure is null ? Task.CompletedTask : Task.FromException(stopFailure);
    }

    /// <summary>
    /// Disposes the host once, then throws the configured dispose failure if any.
    /// </summary>
    [SuppressMessage(
        "Major Code Smell",
        "S3877:Exceptions should not be thrown from unexpected methods",
        Justification = "This test double must simulate a host disposal failure at the process boundary.")]
    public void Dispose()
    {
        if (DisposeCalled)
        {
            return;
        }

        DisposeCalled = true;
        lifecycleEvents.Add("Dispose");

        try
        {
            provider.Dispose();
        }
        finally
        {
            activitySource.Dispose();
            lifetime.Dispose();
        }

        if (disposeFailure is not null)
        {
            throw disposeFailure;
        }
    }

    private Task RunInitialization(Func<CancellationToken, Task> initializationOperation, CancellationToken ct)
    {
        InitializationCalled = true;
        InitializationToken = ct;
        lifecycleEvents.Add("Initialize");

        if (cancelDuringInitialization)
        {
            lifetime.StopApplication();
        }

        return initializationOperation(ct);
    }

    private sealed class TestHostApplicationLifetime : IHostApplicationLifetime, IDisposable
    {
        private readonly CancellationTokenSource stopping = new();

        public CancellationToken ApplicationStarted => CancellationToken.None;

        public CancellationToken ApplicationStopping => stopping.Token;

        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication() => stopping.Cancel();

        public void Dispose() => stopping.Dispose();
    }
}
