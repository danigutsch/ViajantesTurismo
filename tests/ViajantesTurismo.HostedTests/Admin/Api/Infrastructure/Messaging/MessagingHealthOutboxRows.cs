using Npgsql;

namespace ViajantesTurismo.HostedTests.Admin.Api.Infrastructure.Messaging;

/// <summary>Seeds three synthetic rows into a test-owned Admin outbox.</summary>
internal static class MessagingHealthOutboxRows
{
    /// <summary>Seeds two unpublished rows, including a retry, and an older published row.</summary>
    public static async Task Seed(string connectionString, DateTimeOffset oldestPending, CancellationToken ct)
    {
        Guid[] ids = [Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7()];
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var command = dataSource.CreateCommand(
            """
            INSERT INTO messaging.outbox_messages
                ("Id", "EnqueuedAt", "PublishedAt", "PublishAttempts", "EnvelopeSpec", "EnvelopeSpecVersion",
                 "EventId", "Source", "EventType", "Payload", "PayloadEncoding", "LastPublishError",
                 "NextPublishAttemptAt")
            SELECT id, @oldest + (1 - rank) * interval '1 hour',
                CASE WHEN rank = 3 THEN @oldest END,
                CASE WHEN rank = 1 THEN 0 ELSE 1 END,
                'cloudevents', '1.0', id::text, '/test', 'test.messaging.health',
                '{"test":"synthetic-outbox-body"}', 'json',
                CASE WHEN rank = 2 THEN 'synthetic-outbox-error' END,
                CASE WHEN rank = 2 THEN @oldest END
            FROM unnest(@ids) WITH ORDINALITY AS fixture(id, rank)
            """);
        command.Parameters.AddWithValue("ids", ids);
        command.Parameters.AddWithValue("oldest", oldestPending.AddHours(1));
        var affected = await command.ExecuteNonQueryAsync(ct);
        affected.ShouldBe(ids.Length);
    }
}
