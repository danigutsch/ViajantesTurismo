namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Describes an ordered SQL step and the conditions under which it may execute.</summary>
/// <param name="Phase">The lifecycle phase.</param>
/// <param name="Order">The order within that phase.</param>
/// <param name="Script">The manifest-relative SQL file.</param>
/// <param name="Sha256">The SHA-256 digest of the SQL bytes.</param>
/// <param name="Transaction">The transaction policy.</param>
/// <param name="TimeoutSeconds">The maximum execution wait.</param>
/// <param name="LockTimeoutSeconds">The maximum lock acquisition wait.</param>
/// <param name="Destructive">Whether the step requires the contract gate.</param>
/// <param name="Checkpoint">The progress key for a batched data step.</param>
/// <param name="Verify">A postcondition script for a batched or online step.</param>
internal sealed record MigrationStep(
    MigrationPhase Phase,
    int Order,
    string Script,
    string Sha256,
    MigrationTransaction Transaction,
    int TimeoutSeconds,
    int LockTimeoutSeconds,
    bool Destructive,
    string? Checkpoint = null,
    MigrationArtifact? Verify = null);
