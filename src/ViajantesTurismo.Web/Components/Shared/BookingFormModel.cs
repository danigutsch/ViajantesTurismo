using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Web.Components.Shared;

/// <summary>
/// Form model for creating and editing bookings.
/// </summary>
public class BookingFormModel
{
    /// <summary>Tour identifier (required when creating from customer view).</summary>
    public int? TourId { get; set; }

    /// <summary>Customer identifier (required when creating from tour view).</summary>
    public int? CustomerId { get; set; }

    /// <summary>Optional companion customer identifier.</summary>
    public int? CompanionId { get; set; }

    /// <summary>Total price of the booking.</summary>
    [Required(ErrorMessage = "Total price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Total price must be greater than zero")]
    public decimal TotalPrice { get; set; }

    /// <summary>Optional notes about the booking.</summary>
    [MaxLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters")]
    public string? Notes { get; set; }

    /// <summary>Booking status.</summary>
    public BookingStatusDto Status { get; set; } = BookingStatusDto.Pending;

    /// <summary>Payment status.</summary>
    public PaymentStatusDto PaymentStatus { get; set; } = PaymentStatusDto.Unpaid;
}
