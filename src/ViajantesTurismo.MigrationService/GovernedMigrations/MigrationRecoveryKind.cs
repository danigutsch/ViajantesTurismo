namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Identifies the approved way to recover after a failed rollout.</summary>
internal enum MigrationRecoveryKind
{
    /// <summary>Runs reviewed SQL that restores the previous schema and required data.</summary>
    Reversible,
    /// <summary>Runs a new forward migration that compensates for the failed change.</summary>
    Compensating,
    /// <summary>Recovers through a tested backup or point-in-time restore.</summary>
    Restore,
    /// <summary>Recovers using an approved forward repair.</summary>
    ForwardOnly,
}
