namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Supplies trusted request metadata for a document audit record.</summary>
public sealed record DocumentAuditContext(string ActorId, string CorrelationId);
