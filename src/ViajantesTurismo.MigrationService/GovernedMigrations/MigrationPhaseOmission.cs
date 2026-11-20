namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Records why a migration has no step in a lifecycle phase.</summary>
/// <param name="Phase">The omitted phase.</param>
/// <param name="Reason">The reviewed reason for the omission.</param>
internal sealed record MigrationPhaseOmission(MigrationPhase Phase, string Reason);
