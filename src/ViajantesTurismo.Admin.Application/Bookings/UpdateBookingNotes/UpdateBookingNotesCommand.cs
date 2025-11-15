namespace ViajantesTurismo.Admin.Application.Bookings.UpdateBookingNotes;

/// <summary>
/// Command to update a booking's notes.
/// </summary>
public sealed record UpdateBookingNotesCommand(
    Guid BookingId,
    string? Notes);
