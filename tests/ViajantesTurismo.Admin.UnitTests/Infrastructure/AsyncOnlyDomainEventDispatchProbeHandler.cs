using PandaBear.Domain;
using PandaBear.EntityFrameworkCore;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>Domain event dispatch handler that records creation, handling and disposal on a probe and can cancel or fail dispatch, for lifecycle tests.</summary>
internal sealed class AsyncOnlyDomainEventDispatchProbeHandler : IDomainEventDispatchHandler, IAsyncDisposable
{
    private readonly DomainEventDispatchLifecycleProbe probe;

    /// <summary>Creates the handler and records its creation on the probe.</summary>
    public AsyncOnlyDomainEventDispatchProbeHandler(DomainEventDispatchLifecycleProbe probe)
    {
        this.probe = probe;
        probe.RecordCreated(this);
    }

    /// <summary>Records the event on the probe, then cancels the probe source, honors cancellation and throws the probe failure when configured.</summary>
    public async ValueTask Handle(IDomainEvent domainEvent, CancellationToken ct)
    {
        probe.RecordHandled(this, domainEvent, CurrentSaveChangesDbContext.Current);
        if (probe.CancellationSource is not null)
        {
            await probe.CancellationSource.CancelAsync().ConfigureAwait(false);
        }

        ct.ThrowIfCancellationRequested();

        if (probe.DispatchFailure is not null)
        {
            throw probe.DispatchFailure;
        }

    }

    /// <summary>Records the disposal on the probe after yielding.</summary>
    public async ValueTask DisposeAsync()
    {
        await Task.Yield();
        probe.RecordDisposed(this, CurrentSaveChangesDbContext.Current);
    }
}
