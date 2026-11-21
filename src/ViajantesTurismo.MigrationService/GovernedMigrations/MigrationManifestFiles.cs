using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using PandaBear.IO;

namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Reads the manifest used by planning and execution.</summary>
internal static class MigrationManifestFiles
{
    private const int ManifestLimit = 1024 * 1024;

    private static readonly JsonSerializerOptions ManifestOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        AllowDuplicateProperties = false,
        Converters =
        {
            new JsonStringEnumConverter<MigrationPhase>(JsonNamingPolicy.CamelCase, allowIntegerValues: false),
            new JsonStringEnumConverter<MigrationTransaction>(JsonNamingPolicy.CamelCase, allowIntegerValues: false),
            new JsonStringEnumConverter<MigrationRecoveryKind>(JsonNamingPolicy.CamelCase, allowIntegerValues: false),
        },
    };

    /// <summary>Validates the manifest and records the digest of its unmodified bytes.</summary>
    internal static async Task<(MigrationManifest Manifest, string Directory, string Digest)> Read(
        string manifestPath, string sourceRelease, CancellationToken ct)
    {
        var path = Path.GetFullPath(manifestPath);
        var content = await BoundedReader.ReadFile(path, ManifestLimit, ct)
            ?? throw new InvalidDataException("Migration manifest exceeds the size limit.");
        var manifest = Deserialize<MigrationManifest>(content);
        MigrationManifestValidator.Validate(manifest, sourceRelease);

        var directory = Path.GetDirectoryName(path) ?? throw new InvalidDataException("A manifest directory is required.");
        return (manifest, directory, Convert.ToHexString(SHA256.HashData(content)));
    }

    /// <summary>Deserializes the reviewed JSON artifacts using their strict manifest property rules.</summary>
    internal static T Deserialize<T>(ReadOnlySpan<byte> content) where T : class =>
        JsonSerializer.Deserialize<T>(content, ManifestOptions)
        ?? throw new InvalidDataException("A governed migration JSON artifact must be an object.");

    /// <summary>Checks the evidence and any declared recovery scripts without executing them.</summary>
    internal static async Task VerifyRecovery(MigrationRecovery recovery, string directory, CancellationToken ct)
    {
        await MigrationArtifactFiles.Verify(directory, recovery.Evidence, ct);
        if (recovery.Script is { } script)
        {
            _ = await MigrationArtifactFiles.ReadVerifiedSql(directory, script, ct);
        }

        if (recovery.Verify is { } verification)
        {
            _ = await MigrationArtifactFiles.ReadVerifiedSql(directory, verification, ct);
        }
    }
}
