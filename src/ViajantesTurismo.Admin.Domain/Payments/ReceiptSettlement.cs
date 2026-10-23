using PandaBear.BuildingBlocks;
using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Separates gross receipt, fees and net settlement from any subsequently approved EUR credit.</summary>
public sealed class ReceiptSettlement : ValueObject
{
    private ReceiptSettlement(PaymentMoney gross, PaymentMoney fees)
    {
        Gross = gross;
        Fees = fees;
    }
    /// <summary>Gets the gross amount actually received.</summary>
    public PaymentMoney Gross
    {
        get;
    }
    /// <summary>Gets actual fees in the settlement currency.</summary>
    public PaymentMoney Fees
    {
        get;
    }
    /// <summary>Gets gross less fees; it never implies installment credit.</summary>
    public decimal NetAmount => Gross.Amount - Fees.Amount;
    /// <summary>Validates received cash and fees using the explicitly selected currency.</summary>
    public static Result<ReceiptSettlement> Create(Currency currency, decimal gross, decimal fees)
    {
        var grossMoney = PaymentMoney.Create(gross, currency);
        var feeMoney = PaymentMoney.Create(fees, currency);
        return grossMoney.IsFailure || feeMoney.IsFailure || gross <= 0 || fees > gross
            ? FinancialErrors.Invalid("settlement").ConvertError<ReceiptSettlement>() : Result.Ok(new ReceiptSettlement(grossMoney.Value, feeMoney.Value));
    }
    /// <inheritdoc />
    public override string ToString() => "ReceiptSettlement [redacted]";
    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Gross;
        yield return Fees;
    }
}
