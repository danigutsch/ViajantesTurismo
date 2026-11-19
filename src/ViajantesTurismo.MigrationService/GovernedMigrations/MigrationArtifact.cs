namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Identifies an immutable file relative to its manifest.</summary>
/// <param name="Path">The manifest-relative path.</param>
/// <param name="Sha256">The SHA-256 digest of the file bytes.</param>
internal sealed record MigrationArtifact(string Path, string Sha256);
