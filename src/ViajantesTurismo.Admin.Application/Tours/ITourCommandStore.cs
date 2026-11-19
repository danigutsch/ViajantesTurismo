using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Tours;

/// <summary>Loads tracked Tour aggregates for capacity mutations in the command transaction.</summary>
public interface ITourCommandStore
{
    /// <summary>Gets the Tour by ID, holding its row until the current transaction completes.</summary>
    /// <returns>The tracked Tour when found, or an empty option.</returns>
    Task<Option<Tour>> GetById(Guid id, CancellationToken ct);

    /// <summary>Gets the Tour owning the booking, holding its row until the current transaction completes.</summary>
    /// <returns>The tracked Tour when found, or an empty option.</returns>
    Task<Option<Tour>> GetByBookingId(Guid bookingId, CancellationToken ct);

    /// <summary>Checks for another Tour with the same identifier.</summary>
    Task<bool> IdentifierExistsExcluding(string identifier, Guid excludeTourId, CancellationToken ct);
}
