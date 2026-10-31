namespace ViajantesTurismo.Admin.Application.Bookings;

/// <summary>
/// Marks a command that targets one existing booking, so pipeline behaviors can validate its identifier.
/// </summary>
public interface IBookingScopedCommand
{
    /// <summary>
    /// Gets the identifier of the targeted booking.
    /// </summary>
    Guid BookingId
    {
        get;
    }
}
