using System.Security.Cryptography;
using System.Text;
using PandaBear.IO;

namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Checks manifest-relative artifact paths and hashes the file bytes.</summary>
internal static class MigrationArtifactFiles
{
    private const int ScriptLimit = 16 * 1024 * 1024;
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Verifies one manifest entry against its file.</summary>
    internal static async Task Verify(string directory, MigrationArtifact artifact, CancellationToken ct)
    {
        var path = ResolvePath(directory, artifact);
        await using var stream = File.OpenRead(path);
        VerifyDigest(await SHA256.HashDataAsync(stream, ct), artifact.Sha256);
    }

    /// <summary>Verifies the path and bytes of a PostgreSQL script.</summary>
    internal static Task VerifySql(string directory, MigrationArtifact artifact, CancellationToken ct)
    {
        RequireSqlExtension(artifact);
        return Verify(directory, artifact, ct);
    }

    /// <summary>Returns the SQL decoded from the same bytes whose digest is verified.</summary>
    internal static async Task<string> ReadVerifiedSql(string directory, MigrationArtifact artifact, CancellationToken ct)
    {
        RequireSqlExtension(artifact);
        var path = ResolvePath(directory, artifact);
        var content = await BoundedReader.ReadFile(path, ScriptLimit, ct)
            ?? throw new InvalidDataException("Migration script exceeds the size limit.");
        VerifyDigest(SHA256.HashData(content), artifact.Sha256);

        string sql;
        try
        {
            sql = Utf8.GetString(content);
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

    private static string ResolvePath(string directory, MigrationArtifact artifact)
    {
        if (artifact.Sha256.Length != 64 || !artifact.Sha256.All(char.IsAsciiHexDigit))
        {
            throw new InvalidDataException("Artifact digest must be a SHA-256 value.");
        }

        var segments = artifact.Path.Split('/');
        if (segments.Any(static segment => segment.Length == 0 || segment is "." or ".."
            || segment.Any(static character => character is '\\' or ':' || char.IsControl(character))))
        {
            throw new InvalidDataException("Artifact path must stay inside the manifest directory.");
        }

        var path = directory;
        foreach (var segment in segments)
        {
            path = Path.Combine(path, segment);
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException("Artifact paths cannot contain symbolic links.");
            }
        }

        if ((File.GetAttributes(path) & FileAttributes.Directory) != 0)
        {
            throw new InvalidDataException("An artifact must be a file.");
        }

        return path;
    }

    private static void VerifyDigest(byte[] digest, string expected)
    {
        if (!string.Equals(Convert.ToHexString(digest), expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Artifact digest mismatch.");
        }
    }

    private static void RequireSqlExtension(MigrationArtifact artifact)
    {
        if (!string.Equals(Path.GetExtension(artifact.Path), ".sql", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("A migration script must have the .sql extension.");
        }
    }
}
