using Npgsql;

namespace ViajantesTurismo.HostedTests.Admin.Api.Infrastructure.Documents;

/// <summary>Reads document audit records from the database.</summary>
internal static class DocumentAuditMetadataReader
{
    /// <summary>Reads audit records for a document, ordered by time then id.</summary>
    internal static async Task<IReadOnlyList<DocumentAuditEntry>> ReadByDocumentId(
        NpgsqlDataSource dataSource,
        Guid documentId,
        CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT "Operation", "Outcome", "ReasonCode", "DocumentId", "ActorId", "CorrelationId", "BookingId", "DocumentRevision", "OccurredAtUtc", "RetentionExpiresAt"
            FROM "DocumentAuditRecords"
            WHERE "DocumentId" = @documentId
            ORDER BY "OccurredAtUtc", "Id";
            """);
        command.Parameters.AddWithValue("documentId", documentId);

        return await Read(command, ct);
    }

    /// <summary>Reads audit records for a booking, ordered by time then id.</summary>
    internal static async Task<IReadOnlyList<DocumentAuditEntry>> ReadByBookingId(
        NpgsqlDataSource dataSource,
        Guid bookingId,
        CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT "Operation", "Outcome", "ReasonCode", "DocumentId", "ActorId", "CorrelationId", "BookingId", "DocumentRevision", "OccurredAtUtc", "RetentionExpiresAt"
            FROM "DocumentAuditRecords"
            WHERE "BookingId" = @bookingId
            ORDER BY "OccurredAtUtc", "Id";
            """);
        command.Parameters.AddWithValue("bookingId", bookingId);

        return await Read(command, ct);
    }

    /// <summary>
    /// Reads the not-found rejections of an operation recorded since a time. They name no document, so they can only be
    /// found by operation and time.
    /// </summary>
    internal static async Task<IReadOnlyList<DocumentAuditEntry>> ReadUnresolvedDocumentRejections(
        NpgsqlDataSource dataSource,
        string operation,
        DateTime sinceUtc,
        CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT "Operation", "Outcome", "ReasonCode", "DocumentId", "ActorId", "CorrelationId", "BookingId", "DocumentRevision", "OccurredAtUtc", "RetentionExpiresAt"
            FROM "DocumentAuditRecords"
            WHERE "DocumentId" IS NULL
              AND "BookingId" IS NULL
              AND "Operation" = @operation
              AND "Outcome" = 'Rejected'
              AND "ReasonCode" = 'DocumentNotFound'
              AND "OccurredAtUtc" >= @sinceUtc
            ORDER BY "OccurredAtUtc", "Id";
            """);
        command.Parameters.AddWithValue("operation", operation);
        command.Parameters.AddWithValue("sinceUtc", sinceUtc);

        return await Read(command, ct);
    }

    private static async Task<IReadOnlyList<DocumentAuditEntry>> Read(NpgsqlCommand command, CancellationToken ct)
    {
        var audits = new List<DocumentAuditEntry>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            Guid? documentId = await reader.IsDBNullAsync(3, ct) ? null : reader.GetGuid(3);
            Guid? bookingId = await reader.IsDBNullAsync(6, ct) ? null : reader.GetGuid(6);
            int? documentRevision = await reader.IsDBNullAsync(7, ct) ? null : reader.GetInt32(7);
            audits.Add(new DocumentAuditEntry(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                documentId,
                reader.GetString(4),
                reader.GetString(5),
                bookingId,
                documentRevision,
                reader.GetDateTime(8),
                reader.GetDateTime(9)));
        }

        return audits;
    }
}
