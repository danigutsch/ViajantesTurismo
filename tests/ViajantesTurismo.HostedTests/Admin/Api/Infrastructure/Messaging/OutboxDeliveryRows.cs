using Npgsql;

namespace ViajantesTurismo.HostedTests.Admin.Api.Infrastructure.Messaging;

/// <summary>Owns synthetic, isolated Admin outbox rows for the HTTP inspection regression.</summary>
internal sealed class OutboxDeliveryRows(
    string connectionString, Guid[] ids, string eventType, DateTimeOffset enqueuedAt) : IAsyncDisposable
{
    /// <summary>Gets identifiers for a retry, a tied pending row, an older pending row, and a published row.</summary>
    internal IReadOnlyList<Guid> Ids => ids;

    /// <summary>Gets the unique type shared only by this fixture's messages.</summary>
    internal string EventType => eventType;

    /// <summary>Gets the timestamp shared by the retry and the next pending message.</summary>
    internal DateTimeOffset EnqueuedAt => enqueuedAt;

    /// <summary>Seeds fixture-owned rows with lease metadata so that the runtime relay cannot modify them.</summary>
    internal static async Task<OutboxDeliveryRows> Seed(string connectionString, CancellationToken ct)
    {
        var ids = Enumerable.Range(0, 4).Select(static _ => Guid.CreateVersion7()).OrderDescending().ToArray();
        var eventType = $"test.messaging.outbox.{ids[0]:N}";
        var enqueuedAt = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var command = dataSource.CreateCommand(
            """
            INSERT INTO messaging.outbox_messages
                ("Id", "EnqueuedAt", "PublishedAt", "PublishAttempts", "LastPublishAttemptAt", "NextPublishAttemptAt",
                 "EnvelopeSpec", "EnvelopeSpecVersion", "EventId", "Source", "EventType", "Subject", "Payload",
                 "PayloadEncoding", "ExtensionAttributesJson", "LastPublishError", "ClaimedBy", "ClaimedUntil")
            SELECT id,
                CASE WHEN rank = 3 THEN @enqueued - interval '1 hour'
                     WHEN rank = 4 THEN @enqueued + interval '1 minute' ELSE @enqueued END,
                CASE WHEN rank = 4 THEN @enqueued + interval '2 minutes' END,
                CASE WHEN rank = 1 THEN 2 ELSE 0 END,
                CASE WHEN rank = 1 THEN @enqueued + interval '1 minute' END,
                CASE WHEN rank = 1 THEN @enqueued + interval '2 minutes' END,
                'cloudevents', '1.0', id::text, '/test', @event_type, 'synthetic-outbox-subject',
                '{"test":"synthetic-outbox-body"}', 'json', '{"test":"synthetic-outbox-extension"}',
                'synthetic-outbox-error', 'synthetic-outbox-claim', CURRENT_TIMESTAMP + interval '1 hour'
            FROM unnest(@ids) WITH ORDINALITY AS fixture(id, rank)
            """);
        command.Parameters.AddWithValue("ids", ids);
        command.Parameters.AddWithValue("event_type", eventType);
        command.Parameters.AddWithValue("enqueued", enqueuedAt);
        var affected = await command.ExecuteNonQueryAsync(ct);
        affected.ShouldBe(ids.Length);
        return new OutboxDeliveryRows(connectionString, ids, eventType, enqueuedAt);
    }

    /// <summary>Replaces only the synthetic error on the fixture retry row.</summary>
    internal async Task ReplaceRetryError(string error, CancellationToken ct)
    {
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var command = dataSource.CreateCommand(
            """UPDATE messaging.outbox_messages SET "LastPublishError" = @error WHERE "Id" = @id""");
        command.Parameters.AddWithValue("id", ids[0]);
        command.Parameters.AddWithValue("error", error);
        var affected = await command.ExecuteNonQueryAsync(ct);
        affected.ShouldBe(1);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var command = dataSource.CreateCommand(
            """DELETE FROM messaging.outbox_messages WHERE "Id" = ANY(@ids)""");
        command.Parameters.AddWithValue("ids", ids);
        _ = await command.ExecuteNonQueryAsync();
    }
}
