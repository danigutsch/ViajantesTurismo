namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Classifies the declared index name within its PostgreSQL schema.</summary>
internal enum MigrationOnlineIndexState
{
    /// <summary>The index name is available for the declared build.</summary>
    Missing,
    /// <summary>The exact index is live, ready, and valid.</summary>
    Ready,
    /// <summary>The exact index exists and cannot yet be trusted.</summary>
    Invalid,
    /// <summary>A different index or object occupies the index name.</summary>
    Conflict
}
