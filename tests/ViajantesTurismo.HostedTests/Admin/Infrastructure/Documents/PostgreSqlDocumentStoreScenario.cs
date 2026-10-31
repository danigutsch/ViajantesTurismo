using PostgreSqlTestDatabase = PandaBear.Npgsql.Testing.PostgreSqlTestDatabaseLease;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PandaBear.Domain.EntityFrameworkCore;
using PandaBear.EntityFrameworkCore;
using PandaBear.Idempotency;
using PandaBear.Idempotency.EntityFrameworkCore;
using PandaBear.IntegrationTesting;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using ViajantesTurismo.Admin.Infrastructure.DispositionHolds;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Resources;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Documents;

/// <summary>Runs document store behavior against a real PostgreSQL database.</summary>
internal sealed class PostgreSqlDocumentStoreScenario : IAsyncDisposable
{
    private readonly PostgreSqlTestDatabase database;
    private readonly string connectionString;

    private PostgreSqlDocumentStoreScenario(PostgreSqlTestDatabase database)
    {
        this.database = database;
        connectionString = database.ConnectionString;
    }

    /// <summary>Creates a scenario over a new fixture-issued database.</summary>
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "The returned scenario owns and disposes the fixture-issued database.")]
    public static async ValueTask<PostgreSqlDocumentStoreScenario> Create(
        PostgreSqlTestServerFixture fixture,
        CancellationToken ct)
    {
        var database = await fixture.CreateDatabase(ct);
        return new PostgreSqlDocumentStoreScenario(database);
    }

    /// <summary>Migrates the database, seeds confirmed bookings and stores the drafts grouped into lineages.</summary>
    public async ValueTask Seed(params DocumentDraft[] documents)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(TestContext.Current.CancellationToken);
        await SeedBookings(
            dbContext,
            documents.Select(document => document.BookingId),
            BookingStatus.Confirmed,
            TestContext.Current.CancellationToken);
        dbContext.DocumentLineages.AddRange(CreateLineages(documents));
        _ = await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Migrates the database, seeds confirmed bookings and stores the lineages.</summary>
    public async ValueTask Seed(params DocumentLineage[] lineages)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(TestContext.Current.CancellationToken);
        await SeedBookings(
            dbContext,
            lineages.Select(lineage => lineage.BookingId),
            BookingStatus.Confirmed,
            TestContext.Current.CancellationToken);
        dbContext.DocumentLineages.AddRange(lineages);
        _ = await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Migrates the database and stores the audit records.</summary>
    public async ValueTask SeedAudits(params DocumentAuditRecord[] records)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(TestContext.Current.CancellationToken);
        var store = new DocumentAuditStore(dbContext);
        foreach (var record in records)
        {
            store.Add(record);
        }

        await dbContext.SaveEntities(TestContext.Current.CancellationToken);
    }

    /// <summary>Migrates the database and stores one audit record.</summary>
    public ValueTask SeedAudit(DocumentAuditRecord record) => SeedAudits(record);

    /// <summary>Purges drafts expired at the given time and returns how many were removed.</summary>
    public async ValueTask<int> PurgeExpiredDrafts(DateTime now, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return (await new DocumentStore(dbContext).PurgeExpiredDrafts(now, NoHeldLineages.Filter, ct)).Purged;
    }

    /// <summary>Purges expired drafts using a context with retry on failure enabled and returns how many were removed.</summary>
    public async ValueTask<int> PurgeExpiredDraftsWithRetries(DateTime now, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext(enableRetryOnFailure: true);
        return (await new DocumentStore(dbContext).PurgeExpiredDrafts(now, NoHeldLineages.Filter, ct)).Purged;
    }

    /// <summary>Installs a trigger that fails every document lineage delete.</summary>
    public async Task InstallLineageDeleteFailure(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        _ = await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE FUNCTION "FailDocumentLineageDelete"()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                RAISE EXCEPTION 'Injected lineage cleanup failure.';
            END;
            $$;

            CREATE TRIGGER "TR_DocumentLineages_FailDelete"
            BEFORE DELETE ON "DocumentLineages"
            FOR EACH ROW EXECUTE FUNCTION "FailDocumentLineageDelete"();
            """,
            ct);
    }

    /// <summary>Returns whether a purge invalidated a stale loaded lineage so that saving an update to it fails with a concurrency conflict.</summary>
    public async ValueTask<bool> HasPurgeInvalidatedStaleLineage(
        Guid documentId,
        DateTime now,
        CancellationToken ct)
    {
        await using var staleContext = CreateDbContext();
        var staleLineage = await new DocumentStore(staleContext).GetByDocumentId(documentId, ct);

        await using (var purgeContext = CreateDbContext())
        {
            _ = await new DocumentStore(purgeContext).PurgeExpiredDrafts(now, NoHeldLineages.Filter, ct);
        }

        staleLineage.ShouldNotBeNull().UpdateField(
                documentId,
                "greeting",
                "Stale update",
                now.AddMinutes(1),
                DocumentDraftInfrastructureTestData.CreateAuditContext())
            .IsSuccess.ShouldBeTrue();

        try
        {
            _ = await staleContext.SaveChangesAsync(ct);
            return false;
        }
        catch (DbUpdateConcurrencyException)
        {
            return true;
        }
    }

    /// <summary>Loads all drafts with their fields ordered by creation time.</summary>
    public async ValueTask<DocumentDraft[]> GetDocuments(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.DocumentDrafts
            .Include(document => document.Fields)
            .OrderBy(document => document.CreatedAt)
            .ToArrayAsync(ct);
    }

    /// <summary>Counts the stored document lineages.</summary>
    public async ValueTask<int> GetLineageCount(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.DocumentLineages.CountAsync(ct);
    }

    /// <summary>Loads the draft revision with the given document id, or <see langword="null" /> when absent.</summary>
    public async ValueTask<DocumentDraft?> GetDocumentById(Guid id, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var lineage = await new DocumentStore(dbContext).GetByDocumentId(id, ct);
        return lineage?.GetRevision(id);
    }

    /// <summary>Loads the lineage that contains the given document id.</summary>
    public async ValueTask<DocumentLineage?> GetLineageByDocumentId(Guid id, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await new DocumentStore(dbContext).GetByDocumentId(id, ct);
    }

    /// <summary>Reads the audit metadata projection for a document id.</summary>
    public async ValueTask<DocumentAuditMetadata?> GetAuditMetadataById(Guid id, CancellationToken ct)
    {
        await using var dbContext = CreateReadDbContext();
        return await new DocumentQueryService(dbContext).GetAuditMetadataById(id, ct);
    }

    /// <summary>Reads the document query projection for a document id.</summary>
    public async ValueTask<GetDocumentDto?> GetDocumentProjectionById(Guid id, CancellationToken ct)
    {
        await using var dbContext = CreateReadDbContext();
        return await new DocumentQueryService(dbContext).GetById(id, ct);
    }

    /// <summary>Returns whether a lineage update loaded concurrently by two contexts fails with a concurrency conflict.</summary>
    public async ValueTask<bool> HasConcurrentLineageUpdateConflict(Guid documentId, DateTime now, CancellationToken ct)
    {
        await using var firstContext = CreateDbContext();
        await using var secondContext = CreateDbContext();
        var firstLineage = await new DocumentStore(firstContext).GetByDocumentId(documentId, ct);
        var secondLineage = await new DocumentStore(secondContext).GetByDocumentId(documentId, ct);
        var auditContext = DocumentAuditContext.Create(
            "9c5ff2e6-8b35-4f78-9df3-ef15af8e92a4",
            "9a3ca841b4354928861c660a6e4e1b99").Value;

        firstLineage.ShouldNotBeNull().UpdateField(
                documentId,
                "greeting",
                "First update",
                now.AddMinutes(1),
                auditContext)
            .IsSuccess.ShouldBeTrue();
        secondLineage.ShouldNotBeNull().UpdateField(
                documentId,
                "greeting",
                "Second update",
                now.AddMinutes(2),
                auditContext)
            .IsSuccess.ShouldBeTrue();
        _ = await firstContext.SaveChangesAsync(ct);

        try
        {
            _ = await secondContext.SaveChangesAsync(ct);
            return false;
        }
        catch (DbUpdateConcurrencyException)
        {
            return true;
        }
    }

    /// <summary>Races the initial saves of two drafts and returns the conflict each save captured, or <see langword="null" /> when it succeeded.</summary>
    public async Task<Exception?[]> SaveInitialRevisionsConcurrently(
        DocumentDraft first,
        DocumentDraft second,
        CancellationToken ct)
    {
        await using (var setupContext = CreateDbContext())
        {
            await setupContext.Database.MigrateAsync(ct);
            await SeedBookings(
                setupContext,
                [first.BookingId, second.BookingId],
                BookingStatus.Confirmed,
                ct);
        }

        var barrier = new DocumentSaveBarrierInterceptor();
        var firstSave = CaptureConcurrentInitialSave(first, barrier, ct);
        var secondSave = CaptureConcurrentInitialSave(second, barrier, ct);
        try
        {
            await barrier.BothSaving.WaitAsync(TimeSpan.FromSeconds(10), ct);
        }
        finally
        {
            barrier.Release();
        }

        return await Task.WhenAll(firstSave, secondSave);
    }

    /// <summary>Races two replacement revisions of one document and returns the conflict each save captured, or <see langword="null" /> when it succeeded.</summary>
    public async Task<Exception?[]> SaveReplacementRevisionsConcurrently(
        Guid documentId,
        DateTime createdAt,
        CancellationToken ct)
    {
        var barrier = new DocumentSaveBarrierInterceptor();
        var firstSave = CaptureConcurrentReplacementSave(documentId, createdAt, barrier, ct);
        var secondSave = CaptureConcurrentReplacementSave(documentId, createdAt, barrier, ct);
        try
        {
            await barrier.BothSaving.WaitAsync(TimeSpan.FromSeconds(10), ct);
        }
        finally
        {
            barrier.Release();
        }

        return await Task.WhenAll(firstSave, secondSave);
    }

    /// <summary>Races two generation attempts on the same idempotency key and returns both outcomes plus the replayed result.</summary>
    public async Task<(Result<Guid>[] Attempts, Result<Guid> Replay)> GenerateWithSameKeyConcurrently(
        DocumentDraft first,
        DocumentDraft second,
        IdempotencyKey key,
        string requestFingerprint,
        CancellationToken ct)
    {
        await using (var setupContext = CreateDbContext())
        {
            await setupContext.Database.MigrateAsync(ct);
            await SeedBookings(
                setupContext,
                [first.BookingId, second.BookingId],
                BookingStatus.Confirmed,
                ct);
        }

        var barrier = new DocumentSaveBarrierInterceptor();
        var services = new ServiceCollection();
        services.AddSingleton(barrier);
        services.AddDomainEventDispatch<AdminWriteDbContext>();
        services.AddIdempotencyStore<AdminWriteDbContext>();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddPostgreSqlIntegrationEventTransportProducer<AdminWriteDbContext>(IntegrationEventConsumerNames.Catalog);
        services.AddDbContext<AdminWriteDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(serviceProvider.GetRequiredService<DocumentSaveBarrierInterceptor>());
        });
        await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        var scope = IdempotencyScope.From($"admin.documents.generate-contract-draft:{first.BookingId:N}");
        var storedKey = DocumentCommandIdempotency.DigestKey(key);
        var competingAttemptCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var firstOperationScope = serviceProvider.CreateAsyncScope();
        await using var secondOperationScope = serviceProvider.CreateAsyncScope();
        var firstAttempt = CaptureSameKeyGeneration(
            first,
            scope,
            storedKey,
            requestFingerprint,
            competingAttemptCompleted,
            firstOperationScope.ServiceProvider,
            ct);
        var secondAttempt = CaptureSameKeyGeneration(
            second,
            scope,
            storedKey,
            requestFingerprint,
            competingAttemptCompleted,
            secondOperationScope.ServiceProvider,
            ct);
        try
        {
            await barrier.BothSaving.WaitAsync(TimeSpan.FromSeconds(10), ct);
        }
        finally
        {
            barrier.Release();
        }

        var attempts = await Task.WhenAll(firstAttempt, secondAttempt);
        await using var replayScope = serviceProvider.CreateAsyncScope();
        var replayStore = replayScope.ServiceProvider.GetRequiredService<IIdempotencyStore>();
        var replayContext = replayScope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();
        var replayCoordinator = new DocumentCommandIdempotency(replayStore, replayContext, TimeProvider.System);
        var replay = await replayCoordinator.GetExistingResult(scope, storedKey, requestFingerprint, ct)
            ?? throw new InvalidOperationException("The completed document idempotency key did not produce a replay.");
        return (attempts, replay);
    }

    /// <summary>Reads the audit records of a booking ordered by occurrence time.</summary>
    public async Task<DocumentAuditRecord[]> GetDocumentAuditsForBooking(Guid bookingId, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.DocumentAuditRecords
            .Where(record => record.BookingId == bookingId)
            .OrderBy(record => record.OccurredAtUtc)
            .ToArrayAsync(ct);
    }

    /// <summary>Reads the audit records that name a document ordered by occurrence time.</summary>
    public async Task<DocumentAuditRecord[]> GetDocumentAuditsForDocument(Guid documentId, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.DocumentAuditRecords
            .Where(record => record.DocumentId == documentId)
            .OrderBy(record => record.Id)
            .ToArrayAsync(ct);
    }

    /// <summary>Reads the stored idempotency state for the document generation scope of a booking and key, or <see langword="null" /> when absent.</summary>
    public async Task<string?> GetIdempotencyState(
        Guid bookingId,
        IdempotencyKey key,
        CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var scope = $"admin.documents.generate-contract-draft:{bookingId:N}";
        return await dbContext.Database.SqlQueryRaw<string>(
                """
                SELECT "State" AS "Value"
                FROM "messaging"."idempotency_keys"
                WHERE "Scope" = {0}
                  AND "Key" = {1}
                """,
                scope,
                DocumentCommandIdempotency.DigestKey(key).Value)
            .SingleOrDefaultAsync(ct);
    }

    /// <summary>Saves two drafts as lineages to provoke a duplicate revision violation.</summary>
    public async Task SaveDuplicateRevisions(
        DocumentDraft first,
        DocumentDraft second,
        CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(ct);
        await SeedBookings(dbContext, [first.BookingId, second.BookingId], BookingStatus.Confirmed, ct);
        dbContext.DocumentLineages.AddRange(CreateLineages([first, second]));
        await dbContext.SaveEntities(ct);
    }

    /// <summary>Saves a draft forced into an existing lineage to provoke a duplicate revision violation.</summary>
    public async Task SaveDuplicateRevision(
        Guid documentLineageId,
        DocumentDraft duplicate,
        CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        await SeedBookings(dbContext, [duplicate.BookingId], BookingStatus.Confirmed, ct);
        dbContext.DocumentDrafts.Add(duplicate);
        dbContext.Entry(duplicate)
            .Property(document => document.DocumentLineageId)
            .CurrentValue = documentLineageId;

        await dbContext.SaveEntities(ct);
    }

    /// <summary>Saves two drafts whose booking id is made identical to provoke a duplicate booking-and-type violation.</summary>
    public async Task SaveDuplicateBookingTypeRevision(
        DocumentDraft first,
        DocumentDraft second,
        CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(ct);
        await SeedBookings(dbContext, [first.BookingId, second.BookingId], BookingStatus.Confirmed, ct);
        var lineages = CreateLineages([first, second]);
        dbContext.DocumentLineages.AddRange(lineages);
        dbContext.Entry(second)
            .Property(document => document.BookingId)
            .CurrentValue = first.BookingId;

        await dbContext.SaveEntities(ct);
    }

    /// <summary>Saves two tours that share one identifier to provoke a uniqueness violation.</summary>
    public async Task SaveDuplicateTourIdentifier(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(ct);
        var identifier = $"duplicate-document-test-{Guid.CreateVersion7():N}";
        dbContext.Tours.AddRange(
            EntityBuilders.BuildTour(new TourOptions(Identifier: identifier, Name: $"First {identifier}")),
            EntityBuilders.BuildTour(new TourOptions(Identifier: identifier, Name: $"Second {identifier}")));

        await dbContext.SaveEntities(ct);
    }

    /// <summary>Saves a document for a booking seeded with the given status, or with no booking when the status is <see langword="null" />.</summary>
    public async Task SaveDocumentForBookingStatus(
        DocumentDraft document,
        BookingStatus? bookingStatus,
        CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(ct);
        if (bookingStatus is not null)
        {
            await SeedBookings(dbContext, [document.BookingId], bookingStatus.Value, ct);
        }

        dbContext.DocumentLineages.Add(DocumentLineage.Restore([document]));
        await dbContext.SaveEntities(ct);
    }

    /// <summary>Reads one document audit record by id, or <see langword="null" /> when absent.</summary>
    public async ValueTask<DocumentAuditRecord?> GetAuditRecord(Guid id, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.DocumentAuditRecords.SingleOrDefaultAsync(record => record.Id == id, ct);
    }

    /// <summary>Returns the column names of the document audit table in alphabetical order.</summary>
    public async ValueTask<string[]> GetDocumentAuditColumnNames(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(ct);
        return await dbContext.Database.SqlQueryRaw<string>(
                """
                SELECT column_name AS "Value"
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'DocumentAuditRecords'
                ORDER BY column_name
                """)
            .ToArrayAsync(ct);
    }

    /// <summary>Purges expired document audit records and returns how many were removed.</summary>
    public async ValueTask<int> PurgeExpiredAuditRecords(DateTime now, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var removedCount = (await new DocumentAuditStore(dbContext).PurgeExpiredRecords(now, NoHeldLineages.Filter, ct)).Purged;
        _ = await dbContext.SaveChangesAsync(ct);
        return removedCount;
    }

    /// <summary>Runs the audit purge handler at the given time, recording evidence, and returns how many records were removed.</summary>
    public async ValueTask<int> PurgeExpiredAuditRecordsWithEvidence(DateTime now, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateAuditPurgeHandler(dbContext, dbContext, now);
        var auditContext = DocumentAuditContext.Create("system:test-purge", Guid.CreateVersion7().ToString("N")).Value;
        return (await handler.Handle(new PurgeExpiredDocumentAuditsCommand(auditContext), ct)).Purged;
    }

    /// <summary>Runs the audit purge handler with a first commit that fails and returns the failure it rethrew.</summary>
    public async ValueTask<InvalidOperationException> PurgeExpiredAuditRecordsWhenTheFirstCommitFails(
        DateTime now,
        InvalidOperationException commitFailure,
        CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateAuditPurgeHandler(dbContext, new FailingFirstSaveUnitOfWork(commitFailure, dbContext), now);
        var auditContext = DocumentAuditContext.Create("system:test-purge", Guid.CreateVersion7().ToString("N")).Value;
        Func<Task> purge = async () => await handler.Handle(new PurgeExpiredDocumentAuditsCommand(auditContext), ct);
        return await purge.ShouldThrow<InvalidOperationException>();
    }

    /// <summary>Reads the stored purge evidence ordered by time.</summary>
    public async ValueTask<DocumentAuditPurgeRecord[]> GetPurgeEvidence(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.DocumentAuditPurgeRecords.AsNoTracking().OrderBy(record => record.OccurredAtUtc).ToArrayAsync(ct);
    }

    /// <summary>Resets the mutable tables while preserving document audit records.</summary>
    public async Task ResetMutableDataPreservingDocumentAudits(CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await PostgreSqlPublicSchemaReset.Reset(connection, ["DocumentAuditRecords"], ct);
    }

    /// <summary>Overwrites the actor id of one document audit record with raw SQL.</summary>
    public async Task UpdateAuditActor(Guid id, string actorId, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"DocumentAuditRecords\" SET \"ActorId\" = {actorId} WHERE \"Id\" = {id}",
            ct);
    }

    /// <summary>Deletes one document audit record with raw SQL.</summary>
    public async Task DeleteAuditRecord(Guid id, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM \"DocumentAuditRecords\" WHERE \"Id\" = {id}",
            ct);
    }

    /// <summary>Removes all document audit records.</summary>
    public async Task TruncateAuditRecords(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        _ = await dbContext.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE \"DocumentAuditRecords\"",
            ct);
    }

    /// <summary>Inserts an audit record that names a document but carries no lineage stamp, bypassing the store.</summary>
    public async Task InsertAuditRecordWithoutLineageStamp(Guid id, Guid documentId, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "DocumentAuditRecords"
                ("Id", "ActorId", "DocumentId", "Operation", "Outcome", "ReasonCode", "CorrelationId", "OccurredAtUtc", "RetentionExpiresAt")
            VALUES ({id}, 'actor', {documentId}, 'Generate', 'Succeeded', 'None', 'corr', {now}, {now.AddMonths(24)})
            """,
            ct);
    }

    /// <summary>Returns whether the partial retention index on unfinalized drafts exists.</summary>
    public async ValueTask<bool> HasRetentionIndex(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var count = await dbContext.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*)::int AS "Value"
                FROM pg_indexes
                WHERE schemaname = 'public'
                  AND tablename = 'DocumentDrafts'
                  AND indexname = 'IX_DocumentDrafts_RetentionExpiresAt_Unfinalized'
                  AND indexdef LIKE '%WHERE ("FinalizedAt" IS NULL)%'
                """)
            .SingleAsync(ct);

        return count == 1;
    }

    /// <summary>Migrates the database to the latest and inserts one raw idempotency entry.</summary>
    public async Task SeedIdempotencyEntry(
        string scope,
        string key,
        DateTimeOffset startedAt,
        DateTimeOffset? completedAt,
        CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(ct);

        var state = completedAt is null ? "Started" : "Completed";
        _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "messaging"."idempotency_keys" ("Scope", "Key", "State", "StartedAt", "CompletedAt")
            VALUES ({scope}, {key}, {state}, {startedAt}, {completedAt})
            """,
            ct);
    }

    /// <summary>Purges document idempotency entries at or before the cutoff and returns how many were removed.</summary>
    public async Task<int> PurgeExpiredIdempotencyEntries(DateTimeOffset cutoff, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return (await new DocumentIdempotencyRetentionStore(dbContext).PurgeExpiredEntries(cutoff, NoHeldLineages.Filter, ct)).Purged;
    }

    /// <summary>Returns the keys of the remaining idempotency entries.</summary>
    public async Task<string[]> GetIdempotencyKeys(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.Database
            .SqlQueryRaw<string>("""SELECT "Key" AS "Value" FROM "messaging"."idempotency_keys" ORDER BY "Key" """)
            .ToArrayAsync(ct);
    }

    /// <summary>Disposes the fixture-issued database.</summary>
    public async ValueTask DisposeAsync() => await database.DisposeAsync();

    private async Task<Exception?> CaptureConcurrentInitialSave(
        DocumentDraft document,
        DocumentSaveBarrierInterceptor barrier,
        CancellationToken ct)
    {
        await using var dbContext = CreateDbContext(saveBarrier: barrier);
        dbContext.DocumentLineages.Add(DocumentLineage.Restore([document]));
        return await CaptureExpectedConflict(() => dbContext.SaveEntities(ct));
    }

    private async Task<Exception?> CaptureConcurrentReplacementSave(
        Guid documentId,
        DateTime createdAt,
        DocumentSaveBarrierInterceptor barrier,
        CancellationToken ct)
    {
        await using var dbContext = CreateDbContext(saveBarrier: barrier);
        var lineage = await new DocumentStore(dbContext).GetByDocumentId(documentId, ct);
        var loadedLineage = lineage.ShouldNotBeNull();
        var replacement = loadedLineage.CreateRevision(
            documentId,
            DocumentDraftInfrastructureTestData.CreateContent("2"),
            createdAt,
            DocumentDraftInfrastructureTestData.CreateAuditContext());
        replacement.IsSuccess.ShouldBeTrue();
        loadedLineage.ClearDomainEvents();
        return await CaptureExpectedConflict(() => dbContext.SaveEntities(ct));
    }

    private static async Task<Exception?> CaptureExpectedConflict(Func<Task> save)
    {
        try
        {
            await save();
            return null;
        }
        catch (Exception exception) when (exception is DocumentRevisionConflictException or DbUpdateConcurrencyException)
        {
            return exception;
        }
    }

    private static async Task<Result<Guid>> CaptureSameKeyGeneration(
        DocumentDraft document,
        IdempotencyScope scope,
        IdempotencyKey key,
        string requestFingerprint,
        TaskCompletionSource competingAttemptCompleted,
        IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        var idempotencyStore = serviceProvider.GetRequiredService<IIdempotencyStore>();
        var dbContext = serviceProvider.GetRequiredService<AdminWriteDbContext>();
        var coordinator = new DocumentCommandIdempotency(idempotencyStore, dbContext, TimeProvider.System);
        var existing = await coordinator.GetExistingResult(scope, key, requestFingerprint, ct);
        if (existing is { } replay)
        {
            return replay;
        }

        var operationWasInvoked = false;
        var result = await coordinator.Execute(
            scope,
            key,
            requestFingerprint,
            async () =>
            {
                operationWasInvoked = true;
                await competingAttemptCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
                var lineage = DocumentLineage.Restore([document]);
                dbContext.DocumentLineages.Add(lineage);
                var audit = DocumentAuditRecord.Create(
                    Guid.CreateVersion7().ToString("N"),
                    document.Id,
                    document.BookingId,
                    document.Revision,
                    DocumentAuditOperation.Generate,
                    DocumentAuditOutcome.Succeeded,
                    DocumentAuditReasonCode.ManualOperation,
                    Guid.CreateVersion7().ToString("N"),
                    document.CreatedAt).Value;
                dbContext.DocumentAuditRecords.Add(audit);
                return Result.Ok(document.Id);
            },
            ct);

        if (operationWasInvoked)
        {
            return result;
        }

        try
        {
            if (result.IsSuccess)
            {
                return result;
            }

            var auditContext = DocumentAuditContext.Create(
                Guid.CreateVersion7().ToString("N"),
                Guid.CreateVersion7().ToString("N")).Value;
            var auditWriter = new DocumentAuditWriter(
                new DocumentAuditStore(dbContext),
                dbContext,
                TimeProvider.System);
            var auditResult = await auditWriter.Add(
                auditContext,
                DocumentAuditOperation.Generate,
                null,
                document.BookingId,
                null,
                DocumentAuditOutcome.Rejected,
                DocumentAuditReasonCode.StateConflict,
                ct);
            return auditResult.IsFailure ? auditResult.ConvertError<Guid>() : result;
        }
        finally
        {
            competingAttemptCompleted.TrySetResult();
        }
    }

    private static PurgeExpiredDocumentAuditsCommandHandler CreateAuditPurgeHandler(
        AdminWriteDbContext dbContext,
        IUnitOfWork unitOfWork,
        DateTime now)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(now));
        var gate = new DocumentHoldGate(new FakeDispositionHoldGuard(), new InMemoryDispositionHoldStore(), clock);
        return new PurgeExpiredDocumentAuditsCommandHandler(
            new DocumentAuditStore(dbContext),
            unitOfWork,
            new DocumentDispositionTransaction(dbContext),
            gate,
            clock);
    }

    /// <summary>Gets the connection string of the scenario database.</summary>
    internal string ConnectionString => connectionString;

    private AdminWriteDbContext CreateDbContext(
        bool enableRetryOnFailure = false,
        DocumentSaveBarrierInterceptor? saveBarrier = null)
    {
        var services = new ServiceCollection();
        services.AddDomainEventDispatch<AdminWriteDbContext>();
        services.AddIdempotencyStore<AdminWriteDbContext>();
        services.AddAdminDispositionHolds();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddPostgreSqlIntegrationEventTransportProducer<AdminWriteDbContext>(IntegrationEventConsumerNames.Catalog);
        using var provider = services.BuildServiceProvider();
        var configurations = provider.GetServices<IDbContextConfiguration<AdminWriteDbContext>>().ToArray();
        var options = new DbContextOptionsBuilder<AdminWriteDbContext>()
            .UseNpgsql(connectionString, npgsqlOptions =>
            {
                if (enableRetryOnFailure)
                {
                    npgsqlOptions.EnableRetryOnFailure();
                }
            })
            .Options;

        if (saveBarrier is not null)
        {
            options = new DbContextOptionsBuilder<AdminWriteDbContext>(options)
                .AddInterceptors(saveBarrier)
                .Options;
        }

        return new AdminWriteDbContext(options, configurations);
    }

    private AdminReadDbContext CreateReadDbContext()
    {
        var options = new DbContextOptionsBuilder<AdminReadDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AdminReadDbContext(options);
    }

    private static async Task SeedBookings(
        AdminWriteDbContext dbContext,
        IEnumerable<Guid> bookingIds,
        BookingStatus bookingStatus,
        CancellationToken ct)
    {
        foreach (var bookingId in bookingIds.Distinct())
        {
            var tourId = Guid.CreateVersion7();
            var customerId = Guid.CreateVersion7();
            var bookingDate = TimeProvider.System.GetUtcNow().UtcDateTime;
            var tourIdentifier = $"document-fixture-{tourId:N}";

            _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "Tours" (
                    "Id", "Identifier", "Name", "StartDate", "EndDate", "Price",
                    "DoubleRoomSupplementPrice", "RegularBikePrice", "EBikePrice", "Currency",
                    "MinCustomers", "MaxCustomers", "IncludedServices")
                VALUES (
                    {tourId}, {tourIdentifier}, {tourIdentifier}, {bookingDate.AddDays(30)},
                    {bookingDate.AddDays(37)}, 0, 0, 0, 0, 'UsDollar', 1, 1, ARRAY['Document']);
                """,
                ct);

            _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "Booking" (
                    "Id", "TourId", "BasePrice", "RoomType", "RoomAdditionalCost",
                    "PrincipalCustomer_CustomerId", "PrincipalCustomer_BikeType", "PrincipalCustomer_BikePrice",
                    "Discount_Type", "Discount_Amount", "BookingDate", "Status")
                VALUES (
                    {bookingId}, {tourId}, 0, 'DoubleOccupancy', 0, {customerId}, 'Regular', 0, 'None', 0,
                    {bookingDate}, {bookingStatus.ToString()});
                """,
                ct);
        }
    }

    private static DocumentLineage[] CreateLineages(IEnumerable<DocumentDraft> documents) =>
        [.. documents
            .GroupBy(document => document.DocumentLineageId)
            .Select(group => DocumentLineage.Restore(group))];
}
