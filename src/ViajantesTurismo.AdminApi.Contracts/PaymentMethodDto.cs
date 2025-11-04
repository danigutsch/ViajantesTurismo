namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// DTO for payment method.
/// </summary>
public enum PaymentMethodDto
{
    /// <summary>Payment made by another method.</summary>
    Other = 0,

    /// <summary>Payment made by credit card.</summary>
    CreditCard = 1,

    /// <summary>Payment made by bank transfer.</summary>
    BankTransfer = 2,

    /// <summary>Payment made in cash.</summary>
    Cash = 3,

    /// <summary>Payment made by check/cheque.</summary>
    Check = 4,

    /// <summary>Payment made via PayPal.</summary>
    PayPal = 5
}
