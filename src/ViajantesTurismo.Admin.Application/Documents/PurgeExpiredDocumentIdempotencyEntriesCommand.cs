using SharedKernel.Mediator;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Requests removal of expired document idempotency entries.</summary>
public sealed record PurgeExpiredDocumentIdempotencyEntriesCommand : ICommand<DocumentPurgeCounts>
{
    /// <summary>Creates a request to purge entries beyond the configured replay window.</summary>
    public PurgeExpiredDocumentIdempotencyEntriesCommand()
    {
    }
}
