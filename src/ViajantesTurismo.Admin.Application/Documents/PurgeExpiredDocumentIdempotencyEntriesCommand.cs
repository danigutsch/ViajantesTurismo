namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Requests removal of document idempotency entries older than the replay window.</summary>
public sealed record PurgeExpiredDocumentIdempotencyEntriesCommand;
