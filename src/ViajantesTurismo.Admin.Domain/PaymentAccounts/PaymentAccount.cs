using PandaBear.Domain;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Domain.PaymentAccounts;

/// <summary>Owns recipient revisions, explicit bank-check attestations and optional independent reviews.</summary>
public sealed class PaymentAccount : IAggregateRoot<Guid>
{
    private readonly List<PaymentAccountRevision> _revisions = [];
    private readonly List<PaymentAccountReviewState> _reviews = [];
    private readonly List<IDomainEvent> _events = [];

    private PaymentAccount(Guid id, string label)
    {
        Id = id;
        Label = label;
    }

    /// <summary>Gets the stable account identifier.</summary>
    public Guid Id
    {
        get;
    }
    /// <summary>Gets the concurrency version for every mutation, including reviews.</summary>
    public long Version
    {
        get; private set;
    }
    /// <summary>Gets the non-sensitive staff label.</summary>
    public string Label
    {
        get; private set;
    }
    /// <summary>Gets whether new instructions may use the current revision.</summary>
    public PaymentAccountStatus Status
    {
        get; private set;
    }
    /// <summary>Gets the current immutable recipient details and verification evidence.</summary>
    public PaymentAccountRevision Current => _revisions[^1];
    /// <summary>Gets historical revisions, retained after replacement or retirement.</summary>
    public IReadOnlyList<PaymentAccountRevision> Revisions => _revisions.AsReadOnly();
    /// <summary>Gets the effective independent reviewer; earlier revisions' reviews are historical only.</summary>
    public string? ReviewedBy => CurrentReview?.ReviewerId;
    /// <summary>Gets the effective independent review outcome without changing approval.</summary>
    public PaymentAccountReviewOutcome ReviewOutcome => CurrentReview?.Outcome ?? PaymentAccountReviewOutcome.None;
    /// <summary>Gets the current review instant.</summary>
    public DateTimeOffset? ReviewedAt => CurrentReview?.ReviewedAt;
    /// <summary>Gets whether independent review requests staff attention.</summary>
    public bool RequiresAttention => ReviewOutcome == PaymentAccountReviewOutcome.ChangesRequested;
    private PaymentAccountReviewState? CurrentReview => _reviews.LastOrDefault(review => review.Revision == Current.Number);
    private DateTimeOffset LastActionAt => new[] { Current.CreatedAt, Current.VerifiedAt ?? Current.CreatedAt, _reviews.Count == 0 ? Current.CreatedAt : _reviews[^1].ReviewedAt }.Max();

    /// <summary>Creates an unverified draft; creation never approves an account.</summary>
    public static Result<PaymentAccount> Create(string label, PaymentRecipientDetails recipient, string actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(recipient);
        if (!ValidLabel(label) || !ValidActor(actorId) || !Utc(now))
        {
            return PaymentAccountErrors.Invalid("account").ConvertError<PaymentAccount>();
        }
        var account = new PaymentAccount(Guid.CreateVersion7(), label);
        account.AddRevision(recipient, actorId, now);
        account.Record(actorId, now, PaymentAccountReason.DraftCreated);
        return Result.Ok(account);
    }

    /// <summary>Creates a fresh unverified recipient revision and invalidates the effective review.</summary>
    public Result Revise(long expectedVersion, string label, PaymentRecipientDetails recipient, string actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(recipient);
        var check = CanChange(expectedVersion, actorId, now);
        if (check.IsFailure)
        {
            return check;
        }
        if (!ValidLabel(label))
        {
            return PaymentAccountErrors.Invalid("label");
        }
        Label = label;
        AddRevision(recipient, actorId, now);
        Record(actorId, now, PaymentAccountReason.RecipientChanged);
        return Result.Ok();
    }

    /// <summary>Explicitly attests an independent bank/recipient check; the entering Admin may attest.</summary>
    public Result Verify(long expectedVersion, string actorId, DateTimeOffset now,
        PaymentAccountVerificationMethod method, DateTimeOffset checkedAt, bool independentCheckAttested, bool confirmed)
    {
        var check = CanChange(expectedVersion, actorId, now);
        if (check.IsFailure)
        {
            return check;
        }
        if (Status != PaymentAccountStatus.Draft)
        {
            return PaymentAccountErrors.Unavailable();
        }
        if (!confirmed || !independentCheckAttested || !Utc(checkedAt) || checkedAt > now
            || method == PaymentAccountVerificationMethod.None || !Enum.IsDefined(method))
        {
            return PaymentAccountErrors.Invalid("attestation");
        }
        var state = Current.State with
        {
            VerifiedBy = actorId,
            VerifiedAt = now,
            Method = method,
            CheckedAt = checkedAt
        };
        _revisions[^1] = PaymentAccountRevision.FromState(state, Current.Recipient);
        Status = PaymentAccountStatus.Verified;
        Record(actorId, now, PaymentAccountReason.BankCheckConfirmed);
        return Result.Ok();
    }

    /// <summary>Records an optional independent review without implicitly withdrawing an approval.</summary>
    public Result Review(long expectedVersion, string actorId, DateTimeOffset now, PaymentAccountReviewOutcome outcome)
    {
        var check = CanChange(expectedVersion, actorId, now);
        if (check.IsFailure)
        {
            return check;
        }
        if (string.Equals(Current.CreatedBy, actorId, StringComparison.Ordinal))
        {
            return PaymentAccountErrors.IndependentReviewerRequired();
        }
        if (outcome is not (PaymentAccountReviewOutcome.Approved or PaymentAccountReviewOutcome.ChangesRequested))
        {
            return PaymentAccountErrors.Invalid("reviewOutcome");
        }
        _reviews.Add(new PaymentAccountReviewState(Current.Number, actorId, now, outcome));
        Record(actorId, now, outcome == PaymentAccountReviewOutcome.Approved ? PaymentAccountReason.ReviewAccepted : PaymentAccountReason.ReviewChangesRequested);
        return Result.Ok();
    }

    /// <summary>Explicitly withdraws verification by creating a new unverified revision.</summary>
    public Result WithdrawVerification(long expectedVersion, string actorId, DateTimeOffset now, bool confirmed)
    {
        var check = CanChange(expectedVersion, actorId, now);
        if (check.IsFailure)
        {
            return check;
        }
        if (!confirmed)
        {
            return PaymentAccountErrors.Invalid("confirmation");
        }
        if (Status != PaymentAccountStatus.Verified)
        {
            return PaymentAccountErrors.Unavailable();
        }
        AddRevision(Current.Recipient, actorId, now);
        Record(actorId, now, PaymentAccountReason.ApprovalWithdrawn);
        return Result.Ok();
    }

    /// <summary>Retires an account permanently, preventing new instructions and further delivery.</summary>
    public Result Retire(long expectedVersion, string actorId, DateTimeOffset now, PaymentAccountReason reason, bool confirmed)
    {
        var check = CanChange(expectedVersion, actorId, now);
        if (check.IsFailure)
        {
            return check;
        }
        if (!confirmed || reason is not (PaymentAccountReason.NoLongerUsed or PaymentAccountReason.AccountReplaced))
        {
            return PaymentAccountErrors.Invalid("retirement");
        }
        Status = PaymentAccountStatus.Inactive;
        Record(actorId, now, reason);
        return Result.Ok();
    }

    /// <summary>Rechecks a pinned recipient revision at instruction generation, finalization and delivery.</summary>
    public bool CanDeliver(Guid revisionId) => Status == PaymentAccountStatus.Verified && Current.Id == revisionId;
    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _events.AsReadOnly();
    /// <inheritdoc />
    public void ClearDomainEvents() => _events.Clear();
    /// <inheritdoc />
    public override string ToString() => "PaymentAccount [redacted]";

    internal PaymentAccountState ExportState() => new(1, Id, Version, Label, Status, _revisions.Select(revision => revision.State).ToArray(), [.. _reviews]);

    internal static Result<PaymentAccount> Restore(PaymentAccountState? state)
    {
        if (state is null || state.SchemaVersion != 1 || state.Id == Guid.Empty || state.Version < 1 || !ValidLabel(state.Label)
            || !Enum.IsDefined(state.Status) || state.Revisions is not { Length: > 0 } || state.Reviews is null
            || state.Version < state.Revisions.Length + state.Reviews.Length)
        {
            return PaymentAccountErrors.Invalid("storedAccount").ConvertError<PaymentAccount>();
        }
        var account = new PaymentAccount(state.Id, state.Label) { Version = state.Version, Status = state.Status };
        foreach (var revision in state.Revisions)
        {
            var details = PaymentRecipientDetails.Restore(revision.Recipient);
            var verified = revision.VerifiedBy is not null;
            if (details.IsFailure || revision.Id == Guid.Empty || account._revisions.Any(item => item.Id == revision.Id)
                || revision.Number != account._revisions.Count + 1 || !ValidActor(revision.CreatedBy) || !Utc(revision.CreatedAt)
                || account._revisions.Count > 0 && revision.CreatedAt < account.Current.CreatedAt
                || verified && (!ValidActor(revision.VerifiedBy) || !revision.VerifiedAt.HasValue || !Utc(revision.VerifiedAt.Value)
                    || revision.VerifiedAt < revision.CreatedAt || !revision.CheckedAt.HasValue || !Utc(revision.CheckedAt.Value)
                    || revision.CheckedAt > revision.VerifiedAt || revision.Method == PaymentAccountVerificationMethod.None || !Enum.IsDefined(revision.Method))
                || !verified && (revision.VerifiedAt is not null || revision.CheckedAt is not null || revision.Method != PaymentAccountVerificationMethod.None))
            {
                return PaymentAccountErrors.Invalid("storedRevision").ConvertError<PaymentAccount>();
            }
            account._revisions.Add(PaymentAccountRevision.FromState(revision, details.Value));
        }
        if (state.Status == PaymentAccountStatus.Verified && account.Current.VerifiedBy is null
            || state.Status == PaymentAccountStatus.Draft && account.Current.VerifiedBy is not null)
        {
            return PaymentAccountErrors.Invalid("storedVerification").ConvertError<PaymentAccount>();
        }
        foreach (var review in state.Reviews)
        {
            if (review.Revision < 1 || review.Revision > account.Current.Number || !ValidActor(review.ReviewerId) || !Utc(review.ReviewedAt)
                || review.ReviewedAt < account._revisions[review.Revision - 1].CreatedAt
                || review.ReviewerId == account._revisions[review.Revision - 1].CreatedBy
                || review.Outcome is not (PaymentAccountReviewOutcome.Approved or PaymentAccountReviewOutcome.ChangesRequested))
            {
                return PaymentAccountErrors.Invalid("storedReview").ConvertError<PaymentAccount>();
            }
            account._reviews.Add(review);
        }
        return Result.Ok(account);
    }

    private Result CanChange(long expectedVersion, string actorId, DateTimeOffset now)
    {
        if (Version != expectedVersion)
        {
            return PaymentAccountErrors.Changed();
        }
        if (!ValidActor(actorId) || !Utc(now) || now < LastActionAt)
        {
            return PaymentAccountErrors.Invalid("action");
        }
        return Status == PaymentAccountStatus.Inactive ? PaymentAccountErrors.Unavailable() : Result.Ok();
    }

    private void AddRevision(PaymentRecipientDetails recipient, string actorId, DateTimeOffset now)
    {
        var revision = new PaymentAccountRevisionState(Guid.CreateVersion7(), _revisions.Count + 1, recipient.ExportState(), actorId, now, null, null, PaymentAccountVerificationMethod.None, null);
        _revisions.Add(PaymentAccountRevision.FromState(revision, recipient));
        Status = PaymentAccountStatus.Draft;
    }

    private void Record(string actorId, DateTimeOffset now, PaymentAccountReason reason)
    {
        Version++;
        _events.Add(new PaymentAccountLifecycleDomainEvent(Guid.CreateVersion7(), Id, Current.Number, actorId, now, reason, Current.IsSelfApproved));
    }

    private static bool ValidLabel(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= AdminDomainLimits.MaxNameLength
        && value == value.Trim() && !value.Any(char.IsControl);
    private static bool ValidActor(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 256 && !value.Any(char.IsControl);
    private static bool Utc(DateTimeOffset value) => value != default && value.Offset == TimeSpan.Zero;
}
