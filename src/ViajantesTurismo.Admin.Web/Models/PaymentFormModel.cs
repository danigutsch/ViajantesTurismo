using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Web.Models;

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

    public CreatePaymentDto ToDto() => new()
    {
        Amount = Amount!.Value,
        PaymentDate = PaymentDate!.Value,
        Method = Method!.Value,
        ReferenceNumber = ReferenceNumber,
        Notes = Notes
    };
}
