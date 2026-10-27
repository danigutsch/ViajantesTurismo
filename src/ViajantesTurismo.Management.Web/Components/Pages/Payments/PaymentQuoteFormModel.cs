namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

/// <summary>Editable form state for an exchange-rate quote.</summary>
internal sealed class PaymentQuoteFormModel
{
    /// <summary>Agreed exchange rate in Brazilian reais per euro.</summary>
    public decimal BrlPerEuro
    {
        get; set;
    }
    /// <summary>Where the rate came from.</summary>
    public string Source { get; set; } = string.Empty;
    /// <summary>External reference identifying the quote.</summary>
    public string Reference { get; set; } = string.Empty;
    /// <summary>UTC moment the rate was agreed, or null when not set.</summary>
    public DateTime? AgreedAtUtc
    {
        get; set;
    }
    /// <summary>UTC moment the quote stops being valid, or null when it does not expire.</summary>
    public DateTime? ExpiresAtUtc
    {
        get; set;
    }
    /// <summary>Whether the quote is used for a refund rather than a receipt.</summary>
    public bool ForRefund
    {
        get; set;
    }
    public override string ToString() => "PaymentQuoteFormModel [redacted]";
}
