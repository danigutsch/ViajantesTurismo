using PandaBear.BuildingBlocks;
using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>A manually agreed BRL-per-EUR rate with explicit evidence and expiry, never an informational public rate.</summary>
public sealed class AgreedFxQuote : ValueObject
{
    private AgreedFxQuote(decimal brlPerEuro, string source, string reference, DateTimeOffset agreedAt, DateTimeOffset expiresAt)
    {
        BrlPerEuro = brlPerEuro;
        Source = source;
        Reference = reference;
        AgreedAt = agreedAt;
        ExpiresAt = expiresAt;
    }
    /// <summary>Gets the agreed BRL settlement units per EUR.</summary>
    public decimal BrlPerEuro
    {
        get;
    }
    /// <summary>Gets the agreed quote source.</summary>
    public string Source
    {
        get;
    }
    /// <summary>Gets the evidence reference.</summary>
    public string Reference
    {
        get;
    }
    /// <summary>Gets the evidenced UTC agreement instant.</summary>
    public DateTimeOffset AgreedAt
    {
        get;
    }
    /// <summary>Gets the exclusive expiry instant.</summary>
    public DateTimeOffset ExpiresAt
    {
        get;
    }
    /// <summary>Validates agreed terms independently of approval.</summary>
    public static Result<AgreedFxQuote> Create(decimal brlPerEuro, string source, string reference, DateTimeOffset agreedAt, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        if (brlPerEuro <= 0 || decimal.Round(brlPerEuro, 8) != brlPerEuro || !Evidence(source) || !Evidence(reference)
            || agreedAt == default || agreedAt.Offset != TimeSpan.Zero || expiresAt.Offset != TimeSpan.Zero || now.Offset != TimeSpan.Zero
            || agreedAt > now || expiresAt <= agreedAt)
        {
            return FinancialErrors.Invalid("quote").ConvertError<AgreedFxQuote>();
        }
        return Result.Ok(new AgreedFxQuote(brlPerEuro, source, reference, agreedAt, expiresAt));
    }
    /// <summary>Tests validity at a controlled instant; expiry is exclusive.</summary>
    public bool IsTimely(DateTimeOffset instant) => instant >= AgreedAt && instant < ExpiresAt;
    /// <summary>Rounds an agreed EUR settlement to cents with midpoint away from zero.</summary>
    public Result<PaymentMoney> Settle(decimal euroAmount)
    {
        if (euroAmount < 0 || decimal.Round(euroAmount, 2) != euroAmount || BrlPerEuro > 1 && euroAmount > decimal.MaxValue / BrlPerEuro)
        {
            return FinancialErrors.Invalid("settlement").ConvertError<PaymentMoney>();
        }
        return PaymentMoney.Create(decimal.Round(euroAmount * BrlPerEuro, 2, MidpointRounding.AwayFromZero), Currency.Real);
    }
    /// <inheritdoc />
    public override string ToString() => "AgreedFxQuote [redacted]";
    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return BrlPerEuro;
        yield return Source;
        yield return Reference;
        yield return AgreedAt;
        yield return ExpiresAt;
    }
    private static bool Evidence(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 128 && value == value.Trim() && !value.Any(char.IsControl);
}
