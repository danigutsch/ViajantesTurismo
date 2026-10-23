namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Contains full recipient details for explicitly authorized Admin access.</summary>
public sealed record GetPaymentAccountDetailsDto
{
    /// <summary>Gets Account.</summary>
    public required GetPaymentAccountDto Account
    {
        get; init;
    }
    /// <summary>Gets Recipient.</summary>
    public required PaymentRecipientDto Recipient
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "GetPaymentAccountDetailsDto [redacted]";
}
