namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Contains the minimal document metadata required for an audit record.</summary>
public sealed record DocumentAuditMetadata(Guid BookingId, int Revision);
