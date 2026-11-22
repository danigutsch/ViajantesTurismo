using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>A document revision command whose processing claim survives a retry.</summary>
/// <param name="AuditContext">The trusted actor and correlation metadata.</param>
public abstract record IdempotentDocumentCommand(DocumentAuditContext AuditContext) : DocumentRejectionAuditedCommand<Result<Guid>>(AuditContext)
{
    /// <summary>Gets or sets the timestamp of the claim this dispatch acquired.</summary>
    internal DateTimeOffset? ClaimedAt
    {
        get; set;
    }
}
