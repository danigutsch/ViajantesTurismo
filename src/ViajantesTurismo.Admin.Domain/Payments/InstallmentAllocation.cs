using PandaBear.BuildingBlocks;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>A positive EUR credit explicitly selected for an installment or allocation correction target.</summary>
public sealed class InstallmentAllocation : ValueObject
{
    private InstallmentAllocation(Guid targetId, decimal euroAmount)
    {
        TargetId = targetId;
        EuroAmount = euroAmount;
    }
    /// <summary>Gets the selected opaque target.</summary>
    public Guid TargetId
    {
        get;
    }
    /// <summary>Gets the exact EUR amount.</summary>
    public decimal EuroAmount
    {
        get;
    }
    /// <summary>Validates a selected allocation without silently rounding.</summary>
    public static Result<InstallmentAllocation> Create(Guid targetId, decimal euroAmount) => targetId != Guid.Empty && euroAmount > 0 && decimal.Round(euroAmount, 2) == euroAmount
        ? Result.Ok(new InstallmentAllocation(targetId, euroAmount)) : FinancialErrors.Invalid("allocation").ConvertError<InstallmentAllocation>();
    /// <summary>Exports the selection for persistence.</summary>
    internal PaymentSelectionState ExportState() => new(TargetId, EuroAmount);
    /// <inheritdoc />
    public override string ToString() => "InstallmentAllocation [redacted]";
    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TargetId;
        yield return EuroAmount;
    }
}
