using Microsoft.EntityFrameworkCore;
using Npgsql;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Fixtures;
using ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Admin.System.Operations;

/// <summary>Owns synthetic Gallery wakes for the real Operations dashboard.</summary>
/// <param name="ManagementUrl">The Management address of the isolated Catalog browser stack.</param>
/// <param name="ConnectionString">The test-owned Catalog database connection.</param>
/// <param name="DueWakeIds">The two exact wake identifiers to make due.</param>
/// <param name="OldestDueAt">The first persisted retry deadline.</param>
internal sealed record CatalogGalleryDashboardScenario(
    Uri ManagementUrl, string ConnectionString, string[] DueWakeIds, DateTimeOffset OldestDueAt)
{
    /// <summary>Queues two due candidates and a future retry on distinct tours with the same placement sequence.</summary>
    internal static async Task<CatalogGalleryDashboardScenario> Create(AspireSystemTestFixture fixture, CancellationToken ct)
    {
        var app = await fixture.GetCatalogGalleryDashboardBrowserApplication(ct);
        var managementUrl = app.GetEndpoint(ResourceNames.WebApp, "https");
        var connection = await app.GetConnectionString(ResourceNames.CatalogDatabase, ct);
        await using var catalog = await CatalogGalleryIntentPostgreSqlScenario.Create(connection, ct);
        var first = await catalog.SeedDraft("gallery-dashboard-first", "First Gallery draft", authoredGallery: false, ct);
        var second = await catalog.SeedDraft("gallery-dashboard-second", "Second Gallery draft", authoredGallery: false, ct);
        var future = await catalog.SeedDraft("gallery-dashboard-future", "Future Gallery draft", authoredGallery: false, ct);
        var now = DateTimeOffset.FromUnixTimeSeconds(TimeProvider.System.GetUtcNow().ToUnixTimeSeconds());
        var oldestDueAt = now.AddMinutes(-5);

        await using var seed = await catalog.Database.Database.BeginTransactionAsync(ct);
        var (firstWake, firstSequence) = await catalog.QueuePhoto(first.Id, ct);
        var (secondWake, secondSequence) = await catalog.QueuePhoto(second.Id, ct);
        var (futureWake, futureSequence) = await catalog.QueuePhoto(future.Id, ct);
        secondSequence.ShouldBe(firstSequence);
        futureSequence.ShouldBe(firstSequence);

        var dueWakeIds = new[] { firstWake.ToString("D"), secondWake.ToString("D") };
        string[] seededWakeIds = [.. dueWakeIds, futureWake.ToString("D")];
        // Commit the new wakes with a future deadline, so the live relay cannot claim them during setup.
        var staged = await catalog.Database.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE messaging.outbox_messages
            SET "PublishAttempts" = 2, "LastPublishAttemptAt" = {oldestDueAt.AddMinutes(-1)},
                "NextPublishAttemptAt" = {now.AddHours(1)}
            WHERE "EventId" = ANY ({seededWakeIds})
            """, ct);
        staged.ShouldBe(3);
        await seed.CommitAsync(ct);
        return new CatalogGalleryDashboardScenario(managementUrl, connection, dueWakeIds, oldestDueAt);
    }

    /// <summary>Makes two wakes due and keeps them stable while the real page takes its read-only snapshot.</summary>
    internal async Task<CatalogGalleryDashboardWakeFence> HoldDueWakes(CancellationToken ct)
    {
        var observer = new NpgsqlConnection(ConnectionString);
        NpgsqlTransaction? fence = null;
        try
        {
            await observer.OpenAsync(ct);
            fence = await observer.BeginTransactionAsync(ct);
            // Key-share allows a non-key retry deadline update and a plain snapshot read.
            // The relay's FOR UPDATE SKIP LOCKED leaves these two test rows alone.
            await using var hold = new NpgsqlCommand("""
                SELECT count(*) FROM (
                    SELECT "Id" FROM messaging.outbox_messages
                    WHERE "EventId" = ANY (@eventIds)
                    FOR KEY SHARE
                ) AS owned_wakes
                """, observer, fence);
            hold.Parameters.AddWithValue("eventIds", DueWakeIds);
            var held = await hold.ExecuteScalarAsync(ct);
            ((long)held.ShouldNotBeNull()).ShouldBe(2L);

            await using var writer = new NpgsqlConnection(ConnectionString);
            await writer.OpenAsync(ct);
            await using var schedule = new NpgsqlCommand("""
                UPDATE messaging.outbox_messages
                SET "NextPublishAttemptAt" = CASE WHEN "EventId" = @first THEN @oldest ELSE @newest END
                WHERE "EventId" = ANY (@eventIds)
                """, writer);
            schedule.Parameters.AddWithValue("eventIds", DueWakeIds);
            schedule.Parameters.AddWithValue("first", DueWakeIds[0]);
            schedule.Parameters.AddWithValue("oldest", OldestDueAt);
            schedule.Parameters.AddWithValue("newest", OldestDueAt.AddMinutes(1));
            var scheduled = await schedule.ExecuteNonQueryAsync(ct);
            scheduled.ShouldBe(2);
            return new CatalogGalleryDashboardWakeFence(observer, fence);
        }
        catch
        {
            await using var ownedObserver = observer;
            if (fence is not null)
            {
                await fence.DisposeAsync();
            }

            throw;
        }
    }
}
