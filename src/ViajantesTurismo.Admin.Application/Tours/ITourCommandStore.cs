using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Tours;

/// <summary>Loads Tour aggregates with row locks held by the command transaction.</summary>
public interface ITourCommandStore
{
    /// <summary>Gets the Tour by ID, holding its row until the current transaction completes.</summary>
    /// <returns>The tracked Tour when found, or an empty option.</returns>
    Task<Option<Tour>> GetById(Guid id, CancellationToken ct);

    /// <summary>Gets the Tour owning the booking, holding its row until the current transaction completes.</summary>
    /// <returns>The tracked Tour when found, or an empty option.</returns>
    Task<Option<Tour>> GetByBookingId(Guid bookingId, CancellationToken ct);

    /// <summary>Gets the Tour owning the booking and locks both rows for a payment operation.</summary>
    /// <param name="bookingId">The booking to lock after its Tour.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The owning Tour, or null if the booking or Tour could not be found.</returns>
    Task<Tour?> GetByBookingIdForPaymentUpdate(Guid bookingId, CancellationToken ct);

    /// <summary>Checks for another Tour with the same identifier.</summary>
    Task<bool> IdentifierExistsExcluding(string identifier, Guid excludeTourId, CancellationToken ct);
}
