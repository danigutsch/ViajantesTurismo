using System.Text;
using SharedKernel.IO;

namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Checks manifest-relative artifact paths and hashes the file bytes.</summary>
internal static class MigrationArtifactFiles
{
    private const int ScriptLimit = 16 * 1024 * 1024;
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Verifies one manifest entry against its file.</summary>
    internal static Task Verify(string directory, MigrationArtifact artifact, CancellationToken ct) =>
        VerifiedArtifact.Verify(directory, artifact.Path, artifact.Sha256, ct);

    /// <summary>Verifies the path and bytes of a PostgreSQL script.</summary>
    internal static Task VerifySql(string directory, MigrationArtifact artifact, CancellationToken ct)
    {
        RequireSqlExtension(artifact);
        return Verify(directory, artifact, ct);
    }

    /// <summary>Checks the exact script and any declared online operation before using it.</summary>
    internal static async Task<string> ReadStepSql(string directory, MigrationStep step, CancellationToken ct)
    {
        var sql = await ReadVerifiedSql(directory, new MigrationArtifact(step.Script, step.Sha256), ct);
        step.OnlineIndex?.ValidateScript(sql);
        return sql;
    }

    /// <summary>Returns the SQL decoded from the same bytes whose digest is verified.</summary>
    internal static async Task<string> ReadVerifiedSql(string directory, MigrationArtifact artifact, CancellationToken ct)
    {
        RequireSqlExtension(artifact);
        var content = await ReadVerifiedBytes(directory, artifact, ScriptLimit, ct);

        string sql;
        try
        {
            sql = Utf8.GetString(content.Span);
        }
        catch (DecoderFallbackException)
        {
            throw new InvalidDataException("Migration script must be valid UTF-8.");
        }

        if (string.IsNullOrWhiteSpace(sql) || sql[0] == '\uFEFF' || sql.Contains('\0', StringComparison.Ordinal))
        {
            throw new InvalidDataException("Migration script must contain SQL in UTF-8 without a BOM or NUL characters.");
        }

        return sql;
    }

    /// <summary>Returns bounded file bytes after verifying that same content against its digest.</summary>
    internal static Task<ReadOnlyMemory<byte>> ReadVerifiedBytes(string directory, MigrationArtifact artifact, int limit, CancellationToken ct) =>
        VerifiedArtifact.Read(directory, artifact.Path, artifact.Sha256, limit, ct);

    private static void RequireSqlExtension(MigrationArtifact artifact)
    {
        if (!string.Equals(Path.GetExtension(artifact.Path), ".sql", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("A migration script must have the .sql extension.");
        }
    }
}
