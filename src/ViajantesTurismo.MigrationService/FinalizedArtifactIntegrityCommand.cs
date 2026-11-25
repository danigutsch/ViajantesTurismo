using Npgsql;
using ViajantesTurismo.Admin.Infrastructure.Documents;

namespace ViajantesTurismo.MigrationService;

/// <summary>Runs a private, read-only report on retained Admin artifact integrity.</summary>
internal static class FinalizedArtifactIntegrityCommand
{
    private const string ConnectionVariable = "VT_DOCUMENT_ARTIFACT_REPORT_CONNECTION";
    private const int PageSize = 100;

    /// <summary>Runs one validated page request and writes its operator output.</summary>
    internal static async Task<int> Run(string[] args, TextWriter output, TextWriter error, CancellationToken ct)
    {
        Guid? afterId;
        if (args is ["report", "finalized-artifacts"])
        {
            afterId = null;
        }
        else if (args is ["report", "finalized-artifacts", "--after", var cursor] && Guid.TryParseExact(cursor, "D", out var revisionId))
        {
            afterId = revisionId;
        }
        else
        {
            await error.WriteLineAsync("Usage: report finalized-artifacts [--after <revision-id>]".AsMemory(), ct);
            return 2;
        }

        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            await error.WriteLineAsync("A dedicated artifact report PostgreSQL connection is required.".AsMemory(), ct);
            return 2;
        }

        try
        {
            var builder = new NpgsqlDataSourceBuilder(connectionString) { Name = "admin-artifact-report" };
            await using var dataSource = builder.Build();
            var rows = await FinalizedArtifactIntegrityReader.ReadPage(dataSource, afterId, PageSize, ct);
            await output.WriteLineAsync("revision_id\tintegrity".AsMemory(), ct);
            foreach (var row in rows)
            {
                await output.WriteLineAsync($"{row.Id:D}\t{row.Status}".AsMemory(), ct);
            }

            return 0;
        }
        catch (Exception exception) when (exception is NpgsqlException or ArgumentException or TimeoutException)
        {
            await error.WriteLineAsync("The artifact integrity report could not be read.".AsMemory(), ct);
            return 1;
        }
    }
}
