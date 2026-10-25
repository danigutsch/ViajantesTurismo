using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.UnitTests.Application.Branding;

internal sealed class FakeBrandingProjectionStore : IBrandingProjectionStore
{
    private readonly List<BrandingInboxEntry> _pending = [];
    private BrandingProjectionState? _pendingState;
    private BrandingProjectionRecovery? _pendingRecovery;
    private readonly List<BrandingRecoveryAuditEntry> _pendingAudit = [];

    public BrandingProjectionState? Current
    {
        get; set;
    }

    public List<BrandingInboxEntry> Inbox { get; } = [];

    public BrandingProjectionRecovery? Recovery
    {
        get; set;
    }

    public List<BrandingRecoveryAuditEntry> Audit { get; } = [];

    public int SaveCount
    {
        get; private set;
    }

    public Task<BrandingProjectionState?> GetCurrent(CancellationToken ct) => Task.FromResult(Current);

    public Task<bool> WasProcessed(Guid eventId, CancellationToken ct) =>
        Task.FromResult(Inbox.Exists(entry => entry.EventId == eventId));

    public Task<BrandingProjectionRecovery> GetRecovery(CancellationToken ct) =>
        Task.FromResult(Recovery ?? BrandingProjectionRecovery.Ready);

    public Task<bool> IsRetiredEpoch(Guid sourceEpoch, CancellationToken ct) =>
        Task.FromResult(Audit.Exists(entry => entry.Kind == BrandingRecoveryAuditKind.EpochTransition && entry.OldEpoch == sourceEpoch));

    public void SetRecovery(BrandingProjectionRecovery recovery) => _pendingRecovery = recovery;

    public void AppendAudit(BrandingRecoveryAuditEntry entry) => _pendingAudit.Add(entry);

    public void Replace(BrandingProjectionState state) => _pendingState = state;

    public void Record(BrandingInboxEntry entry) => _pending.Add(entry);

    public Task Save(CancellationToken ct)
    {
        SaveCount++;
        Current = _pendingState ?? Current;
        Inbox.AddRange(_pending);
        Recovery = _pendingRecovery ?? Recovery;
        Audit.AddRange(_pendingAudit);
        _pending.Clear();
        _pendingAudit.Clear();
        _pendingState = null;
        _pendingRecovery = null;
        return Task.CompletedTask;
    }
}
