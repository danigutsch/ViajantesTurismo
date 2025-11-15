using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// DTO for creating a payment record.
/// </summary>
public sealed record CreatePaymentDto
{
    /// <summary>The payment amount.</summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than zero.")]
    public required decimal Amount { get; init; }

    /// <summary>The date the payment was made.</summary>
    [Required]
    public required DateTime PaymentDate { get; init; }

    /// <summary>The payment method used.</summary>
    [Required]
    public required PaymentMethodDto Method { get; init; }

    /// <summary>Optional reference number for the payment.</summary>
    [MaxLength(ContractConstants.MaxReferenceNumberLength)]
    public string? ReferenceNumber { get; init; }

    /// <summary>Optional notes about the payment.</summary>
    [MaxLength(ContractConstants.MaxPaymentNotesLength)]
    public string? Notes { get; init; }
}
