using PandaBear.Mediator;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Requests removal of expired unfinalized document drafts.</summary>
public sealed record PurgeExpiredDraftsCommand : ICommand<DocumentPurgeCounts>
{
    /// <summary>Creates a request to purge drafts beyond the configured retention period.</summary>
    public PurgeExpiredDraftsCommand()
    {
    }
}
