using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Reads the current source version from the booking and tour read models.</summary>
internal sealed class DocumentSourceVersionReader(IQueryService queryService) : IDocumentSourceVersionReader
{
    /// <inheritdoc />
    public async Task<Result<string>> Read(Guid bookingId, CancellationToken ct)
    {
        var booking = await queryService.GetBookingById(bookingId, ct);
        if (booking is null)
        {
            return BookingErrors.BookingNotFound().ConvertError<string>();
        }

        var tour = await queryService.GetTourById(booking.TourId, ct);
        if (tour is null)
        {
            return TourErrors.TourNotFound().ConvertError<string>();
        }

        return Result.Ok(ContractDocumentDraftFactory.CreateSourceVersion(booking, tour));
    }
}
