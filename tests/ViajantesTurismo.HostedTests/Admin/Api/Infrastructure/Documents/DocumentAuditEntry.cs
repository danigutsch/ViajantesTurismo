namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Documents;

/// <summary>Document audit record row as read from the database.</summary>
/// <param name="Operation">Audited operation name.</param>
/// <param name="Outcome">Operation outcome.</param>
/// <param name="ReasonCode">Reason code recorded for the outcome.</param>
/// <param name="DocumentId">Document id, or null when the record has none.</param>
/// <param name="ActorId">Id of the actor who performed the operation.</param>
/// <param name="CorrelationId">Correlation id of the request.</param>
/// <param name="BookingId">Booking id, or null when the record has none.</param>
/// <param name="DocumentRevision">Document revision, or null when not applicable.</param>
/// <param name="OccurredAtUtc">UTC time the operation occurred.</param>
/// <param name="RetentionExpiresAt">UTC time the record's retention period ends.</param>
internal sealed record DocumentAuditEntry(
    string Operation,
    string Outcome,
    string ReasonCode,
    Guid? DocumentId,
    string ActorId,
    string CorrelationId,
    Guid? BookingId,
    int? DocumentRevision,
    DateTime OccurredAtUtc,
    DateTime RetentionExpiresAt);
