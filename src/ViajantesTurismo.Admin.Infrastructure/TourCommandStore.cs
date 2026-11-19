using Microsoft.EntityFrameworkCore;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure;

/// <summary>Loads Tour aggregates under a PostgreSQL row lock owned by the command transaction.</summary>
internal sealed class TourCommandStore(AdminWriteDbContext dbContext, ITourStore tours) : ITourCommandStore
{
    /// <inheritdoc />
    public async Task<Option<Tour>> GetById(Guid id, CancellationToken ct)
    {
        return await GetLockedTour(
            $"""
            SELECT "Id" AS "Value"
            FROM "Tours"
            WHERE "Id" = {id}
            FOR UPDATE
            """, ct);
    }

    /// <inheritdoc />
    public async Task<Option<Tour>> GetByBookingId(Guid bookingId, CancellationToken ct)
    {
        return await GetLockedTour(
            $"""
            SELECT "Id" AS "Value"
            FROM "Tours"
            WHERE "Id" = (SELECT "TourId" FROM "Booking" WHERE "Id" = {bookingId})
            FOR UPDATE
            """, ct);
    }

    /// <inheritdoc />
    public async Task<bool> IdentifierExistsExcluding(string identifier, Guid excludeTourId, CancellationToken ct)
    {
        return await tours.IdentifierExistsExcluding(identifier, excludeTourId, ct);
    }

    private async Task<Option<Tour>> GetLockedTour(FormattableString query, CancellationToken ct)
    {
        if (dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Loading a Tour for a command requires an active transaction.");
        }

        var id = await dbContext.Database.SqlQuery<Guid>(query).SingleOrDefaultAsync(ct);
        return id == Guid.Empty ? Option.None<Tour>() : Option.FromNullable(await tours.GetById(id, ct));
    }
}
