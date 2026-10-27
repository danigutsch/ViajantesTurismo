using ViajantesTurismo.Admin.Application.Branding;

namespace ViajantesTurismo.Admin.UnitTests.Application.Branding;

/// <summary>In-memory branding projection store whose staged changes only apply on <c>Save</c>.</summary>
internal sealed class FakeBrandingProjectionStore : IBrandingProjectionStore
{
    private readonly List<BrandingInboxEntry> _pending = [];
    private BrandingProjectionState? _pendingState;
    private BrandingProjectionRecovery? _pendingRecovery;
    private readonly List<BrandingRecoveryAuditEntry> _pendingAudit = [];

    /// <summary>Gets or sets the persisted current projection state.</summary>
    public BrandingProjectionState? Current
    {
        get; set;
    }

    /// <summary>Gets the persisted inbox entries.</summary>
    public List<BrandingInboxEntry> Inbox { get; } = [];

    /// <summary>Gets or sets the persisted recovery state.</summary>
    public BrandingProjectionRecovery? Recovery
    {
        get; set;
    }

    /// <summary>Gets the persisted recovery audit entries.</summary>
    public List<BrandingRecoveryAuditEntry> Audit { get; } = [];

    /// <summary>Gets how many times <c>Save</c> was called.</summary>
    public int SaveCount
    {
        get; private set;
    }

    /// <summary>Returns the persisted current state.</summary>
    public Task<BrandingProjectionState?> GetCurrent(CancellationToken ct) => Task.FromResult(Current);

    /// <summary>Returns whether the inbox holds the event id.</summary>
    public Task<bool> WasProcessed(Guid eventId, CancellationToken ct) =>
        Task.FromResult(Inbox.Exists(entry => entry.EventId == eventId));

    /// <summary>Returns the persisted recovery, or ready when none.</summary>
    public Task<BrandingProjectionRecovery> GetRecovery(CancellationToken ct) =>
        Task.FromResult(Recovery ?? BrandingProjectionRecovery.Ready);

    /// <summary>Returns whether an epoch transition audit entry retired the epoch.</summary>
    public Task<bool> IsRetiredEpoch(Guid sourceEpoch, CancellationToken ct) =>
        Task.FromResult(Audit.Exists(entry => entry.Kind == BrandingRecoveryAuditKind.EpochTransition && entry.OldEpoch == sourceEpoch));

    /// <summary>Stages a recovery state until save.</summary>
    public void SetRecovery(BrandingProjectionRecovery recovery) => _pendingRecovery = recovery;

    /// <summary>Stages an audit entry until save.</summary>
    public void AppendAudit(BrandingRecoveryAuditEntry entry) => _pendingAudit.Add(entry);

    /// <summary>Stages a replacement projection state until save.</summary>
    public void Replace(BrandingProjectionState state) => _pendingState = state;

    /// <summary>Stages an inbox entry until save.</summary>
    public void Record(BrandingInboxEntry entry) => _pending.Add(entry);

    /// <summary>Applies all staged changes and increments the save count.</summary>
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
