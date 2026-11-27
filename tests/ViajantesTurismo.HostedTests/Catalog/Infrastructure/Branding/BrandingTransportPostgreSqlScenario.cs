using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Branding;

/// <summary>Provides direct access to the Catalog-owned integration event transport schema.</summary>
internal static class BrandingTransportPostgreSqlScenario
{
    /// <summary>Creates the Catalog migration context for a fixture-issued database.</summary>
    public static CatalogDbContext CreateDbContext(string connectionString)
    {
        var dbContext = new CatalogDbContextDesignTimeFactory().CreateDbContext([]);
        dbContext.Database.SetConnectionString(connectionString);
        return dbContext;
    }

    /// <summary>Inserts one transport row with a fresh identity and the supplied serialized payload.</summary>
    public static Task<int> InsertMessage(
        CatalogDbContext dbContext,
        string consumer,
        string eventType,
        string payload,
        CancellationToken ct)
    {
        var id = Guid.CreateVersion7();
        return dbContext.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO messaging.transport_messages
                ("Id", "ConsumerName", "ReceivedAt", "ConsumeAttempts", "EnvelopeSpec", "EnvelopeSpecVersion",
                 "EventId", "Source", "EventType", "Payload", "PayloadEncoding")
            VALUES
                ({id}, {consumer}, CURRENT_TIMESTAMP, 0, 'cloudevents', '1.0',
                 {id.ToString()}, '/branding', {eventType}, {payload}, 'json')
            """,
            ct);
    }
}
