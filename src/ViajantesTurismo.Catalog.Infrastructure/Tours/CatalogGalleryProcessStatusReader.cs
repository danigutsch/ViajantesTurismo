using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;

namespace ViajantesTurismo.Catalog.Infrastructure.Tours;

/// <summary>Reads Gallery placement status and retry health using indexed storage metadata.</summary>
internal sealed class CatalogGalleryProcessStatusReader(CatalogDbContext database, NpgsqlDataSource dataSource)
{
    private const string CanonicalWakeIdPattern = "^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$";

    /// <summary>The point lookup for a Catalog-owned tour and sequence.</summary>
    internal const string ReadSql = $$"""
        SELECT intent."Id",
               outcome.event_type,
               COALESCE(outcome.stream_revision, retirement.stream_revision),
               COALESCE(outcome.recorded_at_utc, retirement.recorded_at_utc),
               retirement.stream_revision IS NOT NULL,
               intent."AcceptedAtUtc",
               COALESCE(outcome.event_id, retirement.event_id),
               COALESCE(outcome.causation_event_id, retirement.causation_event_id),
               withdrawal."RetirementId",
               withdrawal."WithdrawnAt"
        FROM "{{CatalogGalleryPlacementIntentEntity.TableName}}" AS intent
        LEFT JOIN LATERAL (
            SELECT event_type, stream_revision, recorded_at_utc, event_id, causation_event_id
            FROM "{{CatalogGalleryProcessStatusIndexes.EventSchema}}".events
            WHERE stream_id = @stream
              AND event_type IN ('{{CatalogEventSerializer.CatalogTourGalleryChangedEventType}}', '{{CatalogEventSerializer.CatalogTourGalleryIntentRejectedEventType}}')
              AND (payload_json ->> 'IntentId') = intent."Id"::text
              AND (payload_json ->> 'IntentSequence') = intent."Sequence"::text
            ORDER BY stream_revision
            LIMIT 1
        ) AS outcome ON true
        LEFT JOIN LATERAL (
            SELECT stream_revision, recorded_at_utc, event_id, causation_event_id,
                   payload_json ->> 'AdminTourId' AS admin_tour_id,
                   payload_json ->> 'RetirementId' AS retirement_id
            FROM "{{CatalogGalleryProcessStatusIndexes.EventSchema}}".events
            WHERE outcome.stream_revision IS NULL
              AND stream_id = @stream
              AND event_type = '{{CatalogEventSerializer.CatalogTourRetiredEventType}}'
            ORDER BY stream_revision
            LIMIT 1
        ) AS retirement ON true
        LEFT JOIN "{{CatalogTourRetirementEntity.TableName}}" AS withdrawal
            ON withdrawal."AdminTourId" = @owner
            AND withdrawal."AdminTourId"::text = retirement.admin_tour_id
            AND withdrawal."RetirementId"::text = retirement.retirement_id
        WHERE intent."CatalogTourId" = @tour AND intent."Sequence" = @sequence
        """;

    // Catalog uses the default outbox mapping. Identify due deliveries before joining the journal,
    // and match canonical envelope IDs before casting them to the journal's UUID key.
    private static readonly string ReadRetryHealthSql = $$"""
        WITH due_wakes AS (
            SELECT CASE WHEN "EventId" ~ @wakeIdPattern THEN "EventId"::uuid END AS id,
                   "NextPublishAttemptAt" AS due_at
            FROM "{{DatabaseSchemas.Messaging}}"."{{new IntegrationEventStorageOptions().OutboxTableName}}"
            WHERE "PublishedAt" IS NULL
              AND "EventType" = @wakeType
              AND "PublishAttempts" >= @minimumFailures
              AND "NextPublishAttemptAt" <= @observedAt
        ),
        owned_wakes AS (
            SELECT intent."Id", intent."CatalogTourId", intent."Sequence", wake.due_at
            FROM due_wakes AS wake
            JOIN "{{CatalogGalleryPlacementIntentEntity.TableName}}" AS intent ON intent."Id" = wake.id
            UNION ALL
            SELECT intent."Id", intent."CatalogTourId", intent."Sequence", wake.due_at
            FROM due_wakes AS wake
            JOIN "{{CatalogGalleryPlacementIntentEntity.TableName}}" AS intent ON intent."BootstrapWakeId" = wake.id
        )
        SELECT count(DISTINCT wake."Id"), min(wake.due_at)
        FROM owned_wakes AS wake
        JOIN "{{CatalogTourReadModelEntityConfiguration.TableName}}" AS tour ON tour."CatalogTourId" = wake."CatalogTourId"
        CROSS JOIN LATERAL (
            SELECT '{{CatalogTourStreamIds.Prefix}}' || replace(tour."AdminTourId"::text, '-', '') AS stream_id
        ) AS owner
        WHERE NOT EXISTS (
            SELECT 1
            FROM "{{CatalogGalleryProcessStatusIndexes.EventSchema}}".events AS outcome
            WHERE outcome.stream_id = owner.stream_id
              AND outcome.event_type IN ('{{CatalogEventSerializer.CatalogTourGalleryChangedEventType}}', '{{CatalogEventSerializer.CatalogTourGalleryIntentRejectedEventType}}')
              AND (outcome.payload_json ->> 'IntentId') = wake."Id"::text
              AND (outcome.payload_json ->> 'IntentSequence') = wake."Sequence"::text
        )
        AND NOT EXISTS (
            SELECT 1
            FROM "{{CatalogGalleryProcessStatusIndexes.EventSchema}}".events AS retirement
            WHERE retirement.stream_id = owner.stream_id
              AND retirement.event_type = '{{CatalogEventSerializer.CatalogTourRetiredEventType}}'
        )
        """;

    /// <summary>Counts distinct pending placements with due repeated retries in one database snapshot.</summary>
    internal async ValueTask<GetCatalogGalleryRetryHealthDto> ReadRetryHealth(DateTimeOffset observedAt, CancellationToken ct)
    {
        var command = dataSource.CreateCommand(ReadRetryHealthSql);
        await using var ownedCommand = command.ConfigureAwait(false);
        command.Parameters.AddWithValue("wakeIdPattern", CanonicalWakeIdPattern);
        command.Parameters.AddWithValue("wakeType", CatalogGalleryPlacementQueuedIntegrationEvent.EventType);
        command.Parameters.AddWithValue("minimumFailures", GetCatalogGalleryPlacementStatusDto.MinimumRepeatedWakeFailures);
        command.Parameters.AddWithValue("observedAt", observedAt.ToUniversalTime());
        var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, ct).ConfigureAwait(false);
        await using var ownedReader = reader.ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            throw new InvalidDataException("The Gallery retry health aggregate returned no row.");
        }

        return new GetCatalogGalleryRetryHealthDto(reader.GetInt64(0), await ReadTimestamp(reader, 1, ct).ConfigureAwait(false));
    }

    /// <summary>Reads the placement identified by its Catalog tour and positive journal sequence.</summary>
    internal async ValueTask<CatalogGalleryProcessStatus?> Read(Guid catalogTourId, long sequence, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequence);

        // The immutable Admin identity supplies the event stream key independently of projection freshness.
        var adminTourId = await database.Set<CatalogTourReadModelEntity>()
            .Where(tour => tour.CatalogTourId == catalogTourId)
            .Select(tour => (Guid?)tour.AdminTourId)
            .SingleOrDefaultAsync(ct).ConfigureAwait(false);
        if (adminTourId is not { } owner)
        {
            return null;
        }

        var command = dataSource.CreateCommand(ReadSql);
        await using var ownedCommand = command.ConfigureAwait(false);
        command.Parameters.AddWithValue("tour", catalogTourId);
        command.Parameters.AddWithValue("sequence", sequence);
        command.Parameters.AddWithValue("stream", CatalogTourStreamIds.FromAdminTourId(owner).Value);
        command.Parameters.AddWithValue("owner", owner);
        var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        await using var ownedReader = reader.ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            return null;
        }

        var eventType = await reader.IsDBNullAsync(1, ct).ConfigureAwait(false) ? null : reader.GetString(1);
        var status = eventType switch
        {
            CatalogEventSerializer.CatalogTourGalleryChangedEventType => CatalogGalleryPlacementStatusDto.Applied,
            CatalogEventSerializer.CatalogTourGalleryIntentRejectedEventType => CatalogGalleryPlacementStatusDto.RejectedAfterPublication,
            null when reader.GetBoolean(4) => CatalogGalleryPlacementStatusDto.ClosedByRetirement,
            null => CatalogGalleryPlacementStatusDto.Pending,
            _ => throw new InvalidDataException("The Gallery placement outcome has an unexpected event type.")
        };

        long? revision = await reader.IsDBNullAsync(2, ct).ConfigureAwait(false) ? null : reader.GetInt64(2);
        var recordedAt = await ReadTimestamp(reader, 3, ct).ConfigureAwait(false);
        var acceptedAt = await ReadTimestamp(reader, 5, ct).ConfigureAwait(false);
        Guid? terminalEventId = await reader.IsDBNullAsync(6, ct).ConfigureAwait(false) ? null : reader.GetGuid(6);
        Guid? causationEventId = await reader.IsDBNullAsync(7, ct).ConfigureAwait(false) ? null : reader.GetGuid(7);
        var withdrawalTime = await ReadTimestamp(reader, 9, ct).ConfigureAwait(false);
        var retirement = withdrawalTime is { } withdrawnAt
            ? new CatalogGalleryRetirementDto(reader.GetGuid(8), withdrawnAt)
            : null;
        return new CatalogGalleryProcessStatus(reader.GetGuid(0), status, revision, recordedAt, acceptedAt, terminalEventId, causationEventId, retirement);
    }

    private static async ValueTask<DateTimeOffset?> ReadTimestamp(NpgsqlDataReader reader, int ordinal, CancellationToken ct) =>
        await reader.IsDBNullAsync(ordinal, ct).ConfigureAwait(false)
            ? null
            : await reader.GetFieldValueAsync<DateTimeOffset>(ordinal, ct).ConfigureAwait(false);
}
