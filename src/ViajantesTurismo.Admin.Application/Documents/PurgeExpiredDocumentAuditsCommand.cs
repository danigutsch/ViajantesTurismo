namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Requests removal of document audit records whose retention period has elapsed.</summary>
public sealed record PurgeExpiredDocumentAuditsCommand;
