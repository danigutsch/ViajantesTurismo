namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Declares the recovery strategy and the reviewed evidence for it.</summary>
/// <param name="Kind">The recovery strategy.</param>
/// <param name="Evidence">The verified rehearsal or procedure document.</param>
/// <param name="Script">The reviewed SQL used for script-based recovery.</param>
internal sealed record MigrationRecovery(
    MigrationRecoveryKind Kind,
    MigrationArtifact Evidence,
    MigrationArtifact? Script = null);
