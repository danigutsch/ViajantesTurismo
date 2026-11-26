using Npgsql;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Admin.Api.Infrastructure.Messaging;

/// <summary>Provides a private set of terminal and nonterminal test transport rows.</summary>
internal sealed class MessagingHealthTransportRows(string connectionString, Guid[] ids) : IAsyncDisposable
{
    /// <summary>Gets the fixture row identifiers in decreasing receive-time order.</summary>
    internal IReadOnlyList<Guid> Ids => ids;

    /// <summary>Seeds terminal failures, a future and a due Catalog retry, and another consumer's older failure.</summary>
    public static async Task<MessagingHealthTransportRows> Seed(string connectionString, CancellationToken ct)
    {
        Guid[] ids = [Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7()];
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var command = dataSource.CreateCommand(
            """
            INSERT INTO messaging.transport_messages
                ("Id", "ConsumerName", "ReceivedAt", "ConsumeAttempts", "EnvelopeSpec", "EnvelopeSpecVersion",
                 "EventId", "Source", "EventType", "Payload", "PayloadEncoding", "LastConsumeError",
                 "NextConsumeAttemptAt", "PoisonedAt")
            SELECT id, CASE WHEN rank = 4 THEN 'test-other-consumer' ELSE @consumer END,
                CURRENT_TIMESTAMP - rank * interval '30 minutes', 1, 'cloudevents', '1.0', id::text, '/test',
                'test.messaging.health', '{"test":"synthetic-message"}', 'json', 'synthetic-error',
                CASE WHEN rank = 3 THEN CURRENT_TIMESTAMP + interval '1 day'
                     WHEN rank = 5 THEN CURRENT_TIMESTAMP - interval '1 minute' END,
                CASE WHEN rank = 4 THEN CURRENT_TIMESTAMP - interval '3 days'
                     WHEN rank IN (1, 2) THEN CURRENT_TIMESTAMP - rank * interval '15 minutes' END
            FROM unnest(@ids) WITH ORDINALITY AS fixture(id, rank)
            """);
        command.Parameters.AddWithValue("ids", ids);
        command.Parameters.AddWithValue("consumer", IntegrationEventConsumerNames.Catalog);
        var affected = await command.ExecuteNonQueryAsync(ct);
        affected.ShouldBe(ids.Length);
        return new MessagingHealthTransportRows(connectionString, ids);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var command = dataSource.CreateCommand(
            """DELETE FROM messaging.transport_messages WHERE "Id" = ANY(@ids)""");
        command.Parameters.AddWithValue("ids", ids);
        _ = await command.ExecuteNonQueryAsync();
    }
}
