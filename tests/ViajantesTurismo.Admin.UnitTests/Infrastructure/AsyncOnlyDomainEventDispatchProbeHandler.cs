using SharedKernel.Domain;
using SharedKernel.DomainEvents;
using SharedKernel.EntityFrameworkCore;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal sealed class AsyncOnlyDomainEventDispatchProbeHandler : IDomainEventDispatchHandler, IAsyncDisposable
{
    private readonly DomainEventDispatchLifecycleProbe probe;

    public AsyncOnlyDomainEventDispatchProbeHandler(DomainEventDispatchLifecycleProbe probe)
    {
        this.probe = probe;
        probe.RecordCreated(this);
    }

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

    public async ValueTask DisposeAsync()
    {
        await Task.Yield();
        probe.RecordDisposed(this, CurrentSaveChangesDbContext.Current);
    }
}
