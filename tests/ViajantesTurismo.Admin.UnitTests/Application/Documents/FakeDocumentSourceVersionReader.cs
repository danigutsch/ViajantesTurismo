using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

/// <summary>Returns a configurable current source version, matching the draft test data by default.</summary>
internal sealed class FakeDocumentSourceVersionReader(string version = "SOURCE-VERSION") : IDocumentSourceVersionReader
{
    /// <summary>Gets or sets the version returned by subsequent reads.</summary>
    public string Version { get; set; } = version;

    /// <summary>Gets or sets the source that reads report as missing, or <see langword="null" /> when none is.</summary>
    public DocumentSourceKind? MissingSource
    {
        get; set;
    }

    /// <inheritdoc />
    public Task<DocumentSourceVersionRead> Read(Guid bookingId, CancellationToken ct) => Task.FromResult(
        MissingSource switch
        {
            DocumentSourceKind.Booking => DocumentSourceVersionRead.Missing(DocumentSourceKind.Booking, BookingErrors.BookingNotFound()),
            DocumentSourceKind.Tour => DocumentSourceVersionRead.Missing(DocumentSourceKind.Tour, TourErrors.TourNotFound()),
            _ => DocumentSourceVersionRead.Found(Version),
        });
}
