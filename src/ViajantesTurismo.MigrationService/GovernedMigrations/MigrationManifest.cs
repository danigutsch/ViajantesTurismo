namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Describes a reviewed deployment migration and its release boundary.</summary>
/// <param name="SchemaVersion">The manifest schema version.</param>
/// <param name="MigrationId">The journal identity within the bounded context.</param>
/// <param name="Context">The owner of the affected database objects.</param>
/// <param name="SourceReleases">The supported starting application releases.</param>
/// <param name="TargetRelease">The application release this migration prepares.</param>
/// <param name="Steps">The ordered script steps.</param>
/// <param name="OmittedPhases">The reviewed phases with no work.</param>
/// <param name="Recovery">The declared recovery strategy.</param>
internal sealed record MigrationManifest(
    int SchemaVersion,
    string MigrationId,
    string Context,
    string[] SourceReleases,
    string TargetRelease,
    MigrationStep[] Steps,
    MigrationPhaseOmission[] OmittedPhases,
    MigrationRecovery Recovery);
