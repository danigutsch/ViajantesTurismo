using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using ViajantesTurismo.MigrationService;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

internal sealed class MigrationProcessTestHost : IHost
{
    private readonly bool cancelDuringSeed;
    private readonly ActivitySource activitySource = new(MigrationRunner.ActivitySourceName);
    private readonly CancellationToken applicationStopping;
    private readonly Exception? disposeFailure;
    private readonly TestHostApplicationLifetime lifetime = new();
    private readonly ServiceProvider provider;
    private readonly Exception? startFailure;
    private readonly Exception? stopFailure;
    private readonly List<string> lifecycleEvents = [];

    public MigrationProcessTestHost(
        Func<CancellationToken, Task> seedOperation,
        bool cancelDuringSeed = false,
        Exception? startFailure = null,
        Exception? stopFailure = null,
        Exception? disposeFailure = null)
    {
        ArgumentNullException.ThrowIfNull(seedOperation);

        this.cancelDuringSeed = cancelDuringSeed;
        this.startFailure = startFailure;
        this.stopFailure = stopFailure;
        this.disposeFailure = disposeFailure;
        applicationStopping = lifetime.ApplicationStopping;

        var services = new ServiceCollection();
        services.AddSingleton<IHostApplicationLifetime>(lifetime);
        services.AddSingleton(serviceProvider => new MigrationRunner(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger.Instance,
            (_, ct) => RunSeed(seedOperation, ct),
            activitySource));
        provider = services.BuildServiceProvider();
    }

    public IServiceProvider Services => provider;

    public CancellationToken ApplicationStopping => applicationStopping;

    public bool DisposeCalled { get; private set; }

    public bool SeedCalled { get; private set; }

    public CancellationToken SeedToken { get; private set; }

    public bool StartCalled { get; private set; }

    public bool StopCalled { get; private set; }

    public IReadOnlyList<string> LifecycleEvents => lifecycleEvents;

    public CancellationToken StartToken { get; private set; }

    public CancellationToken StopToken { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        StartCalled = true;
        StartToken = cancellationToken;
        lifecycleEvents.Add("Start");

        return startFailure is null ? Task.CompletedTask : Task.FromException(startFailure);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        StopCalled = true;
        StopToken = cancellationToken;
        lifecycleEvents.Add("Stop");

        return stopFailure is null ? Task.CompletedTask : Task.FromException(stopFailure);
    }

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

    private Task RunSeed(Func<CancellationToken, Task> seedOperation, CancellationToken ct)
    {
        SeedCalled = true;
        SeedToken = ct;
        lifecycleEvents.Add("Seed");

        if (cancelDuringSeed)
        {
            lifetime.StopApplication();
        }

        return seedOperation(ct);
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
