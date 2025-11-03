using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Admin.Web.Components.Shared;

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

    /// <summary>Room type for the booking.</summary>
    [Required(ErrorMessage = "Room type is required")]
    public RoomTypeDto RoomType { get; set; } = RoomTypeDto.SingleRoom;

    /// <summary>Optional notes about the booking.</summary>
    [MaxLength(ContractConstants.MaxBookingNotesLength, ErrorMessage = "Notes cannot exceed 2000 characters")]
    public string? Notes { get; set; }

    /// <summary>Booking status.</summary>
    public BookingStatusDto Status { get; set; } = BookingStatusDto.Pending;

    /// <summary>Payment status.</summary>
    public PaymentStatusDto PaymentStatus { get; set; } = PaymentStatusDto.Unpaid;
}