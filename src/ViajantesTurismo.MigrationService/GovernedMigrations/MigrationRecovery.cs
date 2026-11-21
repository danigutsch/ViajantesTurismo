namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Declares the recovery strategy and the reviewed evidence for it.</summary>
/// <param name="Kind">The recovery strategy.</param>
/// <param name="Evidence">The verified rehearsal or procedure document.</param>
/// <param name="Script">The reviewed SQL used for script-based recovery.</param>
/// <param name="Verify">The separate read-only evidence gate after recovery SQL.</param>
/// <param name="TimeoutSeconds">The maximum execution wait for recovery scripts.</param>
/// <param name="LockTimeoutSeconds">The maximum lock acquisition wait for recovery scripts.</param>
internal sealed record MigrationRecovery(
    MigrationRecoveryKind Kind,
    MigrationArtifact Evidence,
    MigrationArtifact? Script = null,
    MigrationArtifact? Verify = null,
    int TimeoutSeconds = 60,
    int LockTimeoutSeconds = 30);
