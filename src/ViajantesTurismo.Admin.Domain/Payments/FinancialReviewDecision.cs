using PandaBear.BuildingBlocks;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>An independent review bound to an immutable financial revision; it never changes an approval.</summary>
public sealed class FinancialReviewDecision : ValueObject
{
    private FinancialReviewDecision(Guid targetId, int revision, string reviewerId, DateTimeOffset reviewedAt, bool changesRequested)
    {
        TargetId = targetId;
        Revision = revision;
        ReviewerId = reviewerId;
        ReviewedAt = reviewedAt;
        ChangesRequested = changesRequested;
    }
    /// <summary>Gets the immutable reviewed resource.</summary>
    public Guid TargetId
    {
        get;
    }
    /// <summary>Gets the exact reviewed revision.</summary>
    public int Revision
    {
        get;
    }
    /// <summary>Gets the independent reviewer identifier.</summary>
    public string ReviewerId
    {
        get;
    }
    /// <summary>Gets the controlled review instant.</summary>
    public DateTimeOffset ReviewedAt
    {
        get;
    }
    /// <summary>Gets an attention flag without implying approval withdrawal.</summary>
    public bool ChangesRequested
    {
        get;
    }
    /// <summary>Validates reviewer independence; self-approval remains a separate permitted action.</summary>
    public static Result<FinancialReviewDecision> Create(Guid targetId, int revision, string creatorId, string reviewerId, DateTimeOffset now, bool changesRequested) =>
        targetId != Guid.Empty && revision >= 1 && FinancialApproval.ValidActor(creatorId) && FinancialApproval.ValidActor(reviewerId)
            && !string.Equals(creatorId, reviewerId, StringComparison.Ordinal) && now != default && now.Offset == TimeSpan.Zero
        ? Result.Ok(new FinancialReviewDecision(targetId, revision, reviewerId, now, changesRequested)) : FinancialErrors.Invalid("independentReviewer").ConvertError<FinancialReviewDecision>();
    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TargetId;
        yield return Revision;
        yield return ReviewerId;
        yield return ReviewedAt;
        yield return ChangesRequested;
    }
}
