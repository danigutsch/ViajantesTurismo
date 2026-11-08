using PandaBear.BuildingBlocks;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>A nonnegative, exact two-decimal EUR or BRL monetary value; never silently rounds submitted amounts.</summary>
public sealed class PaymentMoney : ValueObject
{
    private PaymentMoney(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }
    /// <summary>Gets the exact amount.</summary>
    public decimal Amount
    {
        get;
    }
    /// <summary>Gets the settlement or obligation currency.</summary>
    public Currency Currency
    {
        get;
    }
    /// <summary>Validates a canonical amount without changing its value.</summary>
    public static Result<PaymentMoney> Create(decimal amount, Currency currency) => amount >= 0 && decimal.Round(amount, 2) == amount && FinancialCurrencies.IsSupported(currency)
        ? Result.Ok(new PaymentMoney(amount, currency)) : FinancialErrors.Invalid("amount").ConvertError<PaymentMoney>();
    /// <inheritdoc />
    public override string ToString() => "PaymentMoney [redacted]";
    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
