using System.Text.Json;
using Npgsql;
using SharedKernel.EventSourcing;
using ViajantesTurismo.Catalog.Infrastructure.Tours;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Measures the event filtering of the actual Gallery status SQL.</summary>
internal static class CatalogGalleryProcessStatusQueryPlan
{
    /// <summary>Counts filtered PostgreSQL event rows and checks the two expected index choices.</summary>
    internal static async Task<long> CountFilteredEventRows(
        NpgsqlDataSource dataSource, Guid catalogTourId, Guid adminTourId, long sequence, StreamId stream, CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(
            "EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) " + CatalogGalleryProcessStatusReader.ReadSql);
        command.Parameters.AddWithValue("tour", catalogTourId);
        command.Parameters.AddWithValue("sequence", sequence);
        command.Parameters.AddWithValue("stream", stream.Value);
        command.Parameters.AddWithValue("owner", adminTourId);
        var json = (string?)await command.ExecuteScalarAsync(ct)
            ?? throw new InvalidDataException("PostgreSQL returned no plan for the Gallery status query.");
        json.ShouldContain(CatalogGalleryProcessStatusIndexes.OutcomeIndexName, StringComparison.Ordinal);
        json.ShouldContain(CatalogGalleryProcessStatusIndexes.RetirementIndexName, StringComparison.Ordinal);
        using var plan = JsonDocument.Parse(json);
        return CountEventFilters(plan.RootElement[0].GetProperty("Plan"));
    }

    private static long CountEventFilters(JsonElement node)
    {
        long filtered = 0;
        if (node.TryGetProperty("Relation Name", out var relation) && relation.GetString() == "events"
            && node.TryGetProperty("Rows Removed by Filter", out var removed))
        {
            filtered = checked((long)removed.GetDouble() * node.GetProperty("Actual Loops").GetInt64());
        }

        if (node.TryGetProperty("Plans", out var plans))
        {
            foreach (var child in plans.EnumerateArray())
            {
                filtered += CountEventFilters(child);
            }
        }

        return filtered;
    }
}
