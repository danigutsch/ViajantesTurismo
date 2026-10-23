using PandaBear.BuildingBlocks;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>An explicit Admin approval captured separately from draft creation; self-approval is permitted.</summary>
public sealed class FinancialApproval : ValueObject
{
    private FinancialApproval(string actorId, DateTimeOffset approvedAt, FinancialReason reason)
    {
        ActorId = actorId;
        ApprovedAt = approvedAt;
        Reason = reason;
    }
    /// <summary>Gets the authenticated approver identifier.</summary>
    public string ActorId
    {
        get;
    }
    /// <summary>Gets the controlled UTC approval instant.</summary>
    public DateTimeOffset ApprovedAt
    {
        get;
    }
    /// <summary>Gets the required reason code.</summary>
    public FinancialReason Reason
    {
        get;
    }
    /// <summary>Creates approval only after an explicit confirmation of the current summary.</summary>
    public static Result<FinancialApproval> Create(string actorId, DateTimeOffset now, FinancialReason reason, bool confirmed) =>
        confirmed && ValidActor(actorId) && now != default && now.Offset == TimeSpan.Zero && reason != FinancialReason.None && Enum.IsDefined(reason)
            ? Result.Ok(new FinancialApproval(actorId, now, reason)) : FinancialErrors.Invalid("approval").ConvertError<FinancialApproval>();
    internal static bool ValidActor(string? actorId) => !string.IsNullOrWhiteSpace(actorId) && actorId.Length <= 256 && !actorId.Any(char.IsControl);
    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ActorId;
        yield return ApprovedAt;
        yield return Reason;
    }
}
