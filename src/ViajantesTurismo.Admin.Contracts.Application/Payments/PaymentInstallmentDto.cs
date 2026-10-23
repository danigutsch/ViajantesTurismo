namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Defines an exact positive EUR installment and its calendar due date.</summary>
public sealed record PaymentInstallmentDto
{
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
    /// <inheritdoc />
    public override string ToString() => "PaymentInstallmentDto [redacted]";
}
