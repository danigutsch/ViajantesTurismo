using SharedKernel.Results;

namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Contains validated, opaque actor and correlation metadata for document audit evidence.</summary>
public sealed record DocumentAuditContext
{
    private DocumentAuditContext(string actorId, string correlationId)
    {
        ActorId = actorId;
        CorrelationId = correlationId;
    }

    /// <summary>Gets the opaque authenticated actor identifier.</summary>
    public string ActorId { get; }

    /// <summary>Gets the server-generated correlation identifier.</summary>
    public string CorrelationId { get; }

    /// <summary>Creates validated document audit metadata from trusted request values.</summary>
    /// <param name="actorId">The opaque authenticated actor identifier.</param>
    /// <param name="correlationId">The server-generated correlation identifier.</param>
    /// <returns>The validated audit context or a typed validation failure.</returns>
    public static Result<DocumentAuditContext> Create(string actorId, string correlationId)
    {
        if (string.IsNullOrWhiteSpace(actorId) || actorId.Length > DocumentAuditLimits.MaxActorIdLength)
        {
            return DocumentAuditErrors.InvalidActorId().ConvertError<DocumentAuditContext>();
        }

        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > DocumentAuditLimits.MaxCorrelationIdLength)
        {
            return DocumentAuditErrors.InvalidCorrelationId().ConvertError<DocumentAuditContext>();
        }

        return Result.Ok(new DocumentAuditContext(actorId, correlationId));
    }
}
