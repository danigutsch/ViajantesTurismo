using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Saves unapproved EUR terms; approval is a separate action.</summary>
public sealed record CreatePaymentScheduleDto
{
    /// <summary>Gets ExpectedVersion.</summary>
    [Range(0, long.MaxValue)]
    public required long ExpectedVersion
    {
        get; init;
    }
    /// <summary>Gets ObligationEuro.</summary>
    public required decimal ObligationEuro
    {
        get; init;
    }
    /// <summary>Gets Installments.</summary>
    [Required]
    public required IReadOnlyList<PaymentInstallmentDto> Installments
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "CreatePaymentScheduleDto [redacted]";
}
