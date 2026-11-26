using Npgsql;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Exposes the persisted relay metadata used to synchronize the restart test.</summary>
/// <param name="StorageId">The durable outbox row identifier.</param>
/// <param name="EventId">The event identity of the queued placement.</param>
/// <param name="FailedAttempts">The durable failure count.</param>
/// <param name="LastAttemptAt">The recorded time of the most recent attempt.</param>
/// <param name="NextAttemptAt">The recorded retry time, when pending.</param>
/// <param name="PublishedAt">The recorded completion time, when published.</param>
internal sealed record CatalogGalleryWakeDelivery(
    Guid StorageId, string EventId, int FailedAttempts, DateTimeOffset LastAttemptAt,
    DateTimeOffset? NextAttemptAt, DateTimeOffset? PublishedAt)
{
    /// <summary>Waits for the actual hosted relay to persist a failed attempt or successful publication.</summary>
    internal static async Task<CatalogGalleryWakeDelivery> WaitFor(
        NpgsqlDataSource database, Guid intentId, bool published, CancellationToken ct)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));
        using var interval = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
        await using var command = database.CreateCommand("""
            SELECT "Id", "EventId", "PublishAttempts", "LastPublishAttemptAt", "NextPublishAttemptAt", "PublishedAt"
            FROM messaging.outbox_messages
            WHERE "EventId" = @eventId
              AND (CASE WHEN @published THEN "PublishedAt" IS NOT NULL
                        ELSE "PublishedAt" IS NULL AND "PublishAttempts" > 0 END)
            """);
        command.Parameters.AddWithValue("eventId", intentId.ToString("D"));
        command.Parameters.AddWithValue("published", published);
        do
        {
            await using var reader = await command.ExecuteReaderAsync(deadline.Token);
            if (await reader.ReadAsync(deadline.Token))
            {
                var lastAttempt = await reader.GetFieldValueAsync<DateTimeOffset>(3, deadline.Token);
                DateTimeOffset? nextAttempt = await reader.IsDBNullAsync(4, deadline.Token)
                    ? null
                    : await reader.GetFieldValueAsync<DateTimeOffset>(4, deadline.Token);
                DateTimeOffset? publishedAt = await reader.IsDBNullAsync(5, deadline.Token)
                    ? null
                    : await reader.GetFieldValueAsync<DateTimeOffset>(5, deadline.Token);
                return new CatalogGalleryWakeDelivery(reader.GetGuid(0), reader.GetString(1), reader.GetInt32(2),
                    lastAttempt, nextAttempt, publishedAt);
            }
        }
        while (await interval.WaitForNextTickAsync(deadline.Token));

        throw new InvalidOperationException("The Catalog relay did not reach the requested outbox state.");
    }
}
