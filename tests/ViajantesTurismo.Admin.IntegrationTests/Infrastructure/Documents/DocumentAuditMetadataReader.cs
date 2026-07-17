using Npgsql;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Documents;

internal static class DocumentAuditMetadataReader
{
    internal static async Task<IReadOnlyList<DocumentAuditEntry>> ReadByDocumentId(
        NpgsqlDataSource dataSource,
        Guid documentId,
        CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT "Operation", "Outcome", "ReasonCode", "DocumentId", "ActorId", "CorrelationId", "BookingId", "DocumentRevision"
            FROM "DocumentAuditRecords"
            WHERE "DocumentId" = @documentId
            ORDER BY "OccurredAtUtc";
            """);
        command.Parameters.AddWithValue("documentId", documentId);

        return await Read(command, ct);
    }

    internal static async Task<IReadOnlyList<DocumentAuditEntry>> ReadByBookingId(
        NpgsqlDataSource dataSource,
        Guid bookingId,
        CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT "Operation", "Outcome", "ReasonCode", "DocumentId", "ActorId", "CorrelationId", "BookingId", "DocumentRevision"
            FROM "DocumentAuditRecords"
            WHERE "BookingId" = @bookingId
            ORDER BY "OccurredAtUtc";
            """);
        command.Parameters.AddWithValue("bookingId", bookingId);

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
                documentRevision));
        }

        return audits;
    }
}
