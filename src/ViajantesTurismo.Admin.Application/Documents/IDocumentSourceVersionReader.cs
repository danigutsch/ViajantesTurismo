namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Reads the current source-data version a contract draft for a booking would be generated from.</summary>
public interface IDocumentSourceVersionReader
{
    /// <summary>Reads the current source version for a booking, or a failure naming the source that is gone.</summary>
    /// <param name="bookingId">The booking that sourced the document.</param>
    /// <param name="ct">The cancellation token.</param>
    Task<DocumentSourceVersionRead> Read(Guid bookingId, CancellationToken ct);
}
