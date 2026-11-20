namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Declares how a step commits its work.</summary>
internal enum MigrationTransaction
{
    /// <summary>Commits the step and its journal state in a single transaction.</summary>
    Atomic,
    /// <summary>Commits bounded data batches with durable progress checkpoints.</summary>
    Batched,
    /// <summary>Runs nontransactional DDL with an explicit postcondition.</summary>
    Online,
}
