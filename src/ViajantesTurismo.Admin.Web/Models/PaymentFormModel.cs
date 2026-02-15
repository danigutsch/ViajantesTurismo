using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Web.Models;

/// <summary>
/// Form model for creating payments. Provides mutable properties for Blazor binding
/// and converts to immutable CreatePaymentDto for API submission.
/// Uses nullable properties to support uninitialized form state.
/// </summary>
public sealed class PaymentFormModel
{
    [Required(ErrorMessage = "Payment amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than zero")]
    public decimal? Amount { get; set; }

    [Required(ErrorMessage = "Payment date is required")]
    public DateTime? PaymentDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Payment method is required")]
    public PaymentMethodDto? Method { get; set; }

    [MaxLength(ContractConstants.MaxReferenceNumberLength)]
    public string? ReferenceNumber { get; set; }

    [MaxLength(ContractConstants.MaxPaymentNotesLength)]
    public string? Notes { get; set; }

    /// <summary>
    /// Converts this form model to a CreatePaymentDto for API submission.
    /// </summary>
    public CreatePaymentDto ToDto() => new()
    {
        Amount = Amount!.Value,
        PaymentDate = DateTime.SpecifyKind(PaymentDate!.Value, DateTimeKind.Utc),
        Method = Method!.Value,
        ReferenceNumber = ReferenceNumber,
        Notes = Notes
    };
}
