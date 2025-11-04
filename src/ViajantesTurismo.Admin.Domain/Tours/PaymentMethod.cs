namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Represents the payment method used for a payment.
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Payment made by credit card.
    /// </summary>
    CreditCard = 0,

    /// <summary>
    /// Payment made by bank transfer.
    /// </summary>
    BankTransfer = 1,

    /// <summary>
    /// Payment made in cash.
    /// </summary>
    Cash = 2,

    /// <summary>
    /// Payment made by check/cheque.
    /// </summary>
    Check = 3,

    /// <summary>
    /// Payment made via PayPal.
    /// </summary>
    PayPal = 4,

    /// <summary>
    /// Payment made by other method.
    /// </summary>
    Other = 5
}