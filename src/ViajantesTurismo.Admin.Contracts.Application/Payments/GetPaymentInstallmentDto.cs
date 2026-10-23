namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Contains an immutable installment and its active EUR allocation balance.</summary>
public sealed record GetPaymentInstallmentDto
{
    /// <summary>Gets Id.</summary>
    public required Guid Id
    {
        get; init;
    }
    /// <summary>Gets EuroAmount.</summary>
    public required decimal EuroAmount
    {
        get; init;
    }
    /// <summary>Gets DueDate.</summary>
    public required DateOnly DueDate
    {
        get; init;
    }
    /// <summary>Gets AllocatedEuro.</summary>
    public required decimal AllocatedEuro
    {
        get; init;
    }
    /// <summary>Gets OutstandingEuro.</summary>
    public required decimal OutstandingEuro
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "GetPaymentInstallmentDto [redacted]";
}
