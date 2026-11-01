using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Reads the current source version from the booking and tour read models.</summary>
internal sealed class DocumentSourceVersionReader(IQueryService queryService) : IDocumentSourceVersionReader
{
    /// <inheritdoc />
    public async Task<DocumentSourceVersionRead> Read(Guid bookingId, CancellationToken ct)
    {
        var booking = await queryService.GetBookingById(bookingId, ct);
        if (booking is null)
        {
            return DocumentSourceVersionRead.Missing(DocumentSourceKind.Booking, BookingErrors.BookingNotFound());
        }

        var tour = await queryService.GetTourById(booking.TourId, ct);
        if (tour is null)
        {
            return DocumentSourceVersionRead.Missing(DocumentSourceKind.Tour, TourErrors.TourNotFound());
        }

        return DocumentSourceVersionRead.Found(ContractDocumentDraftFactory.CreateSourceVersion(booking, tour));
    }
}
