using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.UnitTests.Application.Branding;

internal sealed class FakeBrandingProjectionStore : IBrandingProjectionStore
{
    private readonly List<BrandingInboxEntry> _pending = [];
    private BrandingProjectionState? _pendingState;

    public BrandingProjectionState? Current
    {
        get; private set;
    }

    public List<BrandingInboxEntry> Inbox { get; } = [];

    public int SaveCount
    {
        get; private set;
    }

    public Task<BrandingProjectionState?> GetCurrent(CancellationToken ct) => Task.FromResult(Current);

    public Task<bool> WasProcessed(Guid eventId, CancellationToken ct) =>
        Task.FromResult(Inbox.Exists(entry => entry.EventId == eventId));

    public void Replace(BrandingProjectionState state) => _pendingState = state;

    public void Record(BrandingInboxEntry entry) => _pending.Add(entry);

    public Task Save(CancellationToken ct)
    {
        SaveCount++;
        Current = _pendingState ?? Current;
        Inbox.AddRange(_pending);
        _pending.Clear();
        _pendingState = null;
        return Task.CompletedTask;
    }
}
