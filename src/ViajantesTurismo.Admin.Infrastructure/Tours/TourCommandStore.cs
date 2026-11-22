using Microsoft.EntityFrameworkCore;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure.Tours;

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
        return await GetLockedTour(BookingTourLockQuery(bookingId), ct);
    }

    /// <inheritdoc />
    public async Task<Tour?> GetByBookingIdForPaymentUpdate(Guid bookingId, CancellationToken ct)
    {
        var tourId = await GetLockedTourId(BookingTourLockQuery(bookingId), ct);
        if (tourId == Guid.Empty)
        {
            return null;
        }

        var lockedBookingId = await dbContext.Database.SqlQuery<Guid>(
            $"""
            SELECT "Id" AS "Value"
            FROM "Booking"
            WHERE "Id" = {bookingId}
            FOR UPDATE
            """).SingleOrDefaultAsync(ct);
        return lockedBookingId == Guid.Empty ? null : await tours.GetById(tourId, ct);
    }

    /// <inheritdoc />
    public async Task<bool> IdentifierExistsExcluding(string identifier, Guid excludeTourId, CancellationToken ct)
    {
        return await tours.IdentifierExistsExcluding(identifier, excludeTourId, ct);
    }

    private async Task<Option<Tour>> GetLockedTour(FormattableString query, CancellationToken ct)
    {
        var id = await GetLockedTourId(query, ct);
        return id == Guid.Empty ? Option.None<Tour>() : Option.FromNullable(await tours.GetById(id, ct));
    }

    private Task<Guid> GetLockedTourId(FormattableString query, CancellationToken ct)
    {
        if (dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Loading a Tour for a command requires an active transaction.");
        }

        return dbContext.Database.SqlQuery<Guid>(query).SingleOrDefaultAsync(ct);
    }

    private static FormattableString BookingTourLockQuery(Guid bookingId) =>
        $"""
        SELECT "Id" AS "Value"
        FROM "Tours"
        WHERE "Id" = (SELECT "TourId" FROM "Booking" WHERE "Id" = {bookingId})
        FOR UPDATE
        """;
}
