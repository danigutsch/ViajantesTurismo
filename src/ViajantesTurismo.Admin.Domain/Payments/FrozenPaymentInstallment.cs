using PandaBear.BuildingBlocks;

namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>An immutable installment settlement snapshot with its explicit cent-rounding adjustment.</summary>
public sealed class FrozenPaymentInstallment : ValueObject
{
    private readonly PaymentInstructionInstallmentState _state;
    private FrozenPaymentInstallment(PaymentInstructionInstallmentState state) => _state = state;
    /// <summary>Gets the selected installment.</summary>
    public Guid InstallmentId => _state.InstallmentId;
    /// <summary>Gets the civil calendar due date.</summary>
    public DateOnly DueDate => _state.DueDate;
    /// <summary>Gets the selected outstanding EUR amount.</summary>
    public decimal EuroAmount => _state.EuroAmount;
    /// <summary>Gets the exact settlement cents assigned to this installment.</summary>
    public decimal SettlementAmount => _state.SettlementAmount;
    /// <summary>Gets the explicit difference from independently rounding this installment away from zero.</summary>
    public decimal RoundingAdjustment => _state.RoundingAdjustment;
    /// <summary>Wraps a stored installment snapshot.</summary>
    internal static FrozenPaymentInstallment FromState(PaymentInstructionInstallmentState state) => new(state);
    /// <inheritdoc />
    public override string ToString() => "FrozenPaymentInstallment [redacted]";
    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return _state;
    }
}
