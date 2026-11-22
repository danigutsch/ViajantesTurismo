using System.Text.Json.Serialization;

namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Provides typed JSON metadata for migration manifests and retirement receipts.</summary>
[JsonSerializable(typeof(MigrationManifest))]
[JsonSerializable(typeof(MigrationRetirementReceipt))]
internal sealed partial class MigrationArtifactJsonContext : JsonSerializerContext;
