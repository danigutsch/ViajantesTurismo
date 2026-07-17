namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Documents;

internal sealed record DocumentAuditEntry(
    string Operation,
    string Outcome,
    string ReasonCode,
    Guid? DocumentId,
    string ActorId,
    string CorrelationId,
    Guid? BookingId,
    int? DocumentRevision);
