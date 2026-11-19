using ViajantesTurismo.Admin.Application.Documents;

namespace ViajantesTurismo.Admin.Testing.Fakes;

/// <summary>Fails a payment fixture if it reads booking contract source data.</summary>
public sealed class UnexpectedContractSourceVersionReader : IDocumentSourceVersionReader
{
    /// <inheritdoc />
    public Task<DocumentSourceVersionRead> Read(Guid bookingId, CancellationToken ct)
    {
        throw new InvalidOperationException("Payment tests do not read booking contract sources.");
    }
}
