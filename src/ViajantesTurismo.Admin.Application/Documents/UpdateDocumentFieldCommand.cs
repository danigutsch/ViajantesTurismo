using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Requests a staff override for one document field.</summary>
public sealed record UpdateDocumentFieldCommand(
    Guid DocumentId,
    string FieldId,
    string? Value,
    DocumentAuditContext? AuditContext = null);
