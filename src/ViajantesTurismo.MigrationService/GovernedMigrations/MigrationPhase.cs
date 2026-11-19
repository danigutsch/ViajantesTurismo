namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Defines the order of the migration lifecycle.</summary>
internal enum MigrationPhase
{
    /// <summary>Adds schema elements while preserving the previous release.</summary>
    Expand,
    /// <summary>Backfills or transforms existing data.</summary>
    Data,
    /// <summary>Checks the migration and application postconditions.</summary>
    Verify,
    /// <summary>Removes obsolete structures after the previous release is retired.</summary>
    Contract,
}
