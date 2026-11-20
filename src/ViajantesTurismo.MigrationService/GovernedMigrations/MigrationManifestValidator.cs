namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Checks the migration lifecycle before any artifacts are accepted for execution.</summary>
internal static class MigrationManifestValidator
{
    /// <summary>Validates the release boundary, phase order, and recovery policy.</summary>
    internal static void Validate(MigrationManifest manifest, string sourceRelease)
    {
        if (manifest.SchemaVersion != 1
            || !IsIdentifier(manifest.MigrationId)
            || !IsIdentifier(manifest.Context)
            || !IsIdentifier(manifest.TargetRelease))
        {
            throw new InvalidDataException("Invalid migration identity or manifest version.");
        }

        if (manifest.SourceReleases.Length == 0
            || !manifest.SourceReleases.All(IsIdentifier)
            || manifest.SourceReleases.Distinct(StringComparer.Ordinal).Count() != manifest.SourceReleases.Length)
        {
            throw new InvalidDataException("Source releases must be valid and unique.");
        }

        if (!manifest.SourceReleases.Contains(sourceRelease, StringComparer.Ordinal))
        {
            throw new InvalidDataException("Source release is outside the manifest window.");
        }

        ValidateSteps(manifest);
        if (manifest.Recovery.Kind is MigrationRecoveryKind.Reversible or MigrationRecoveryKind.Compensating
            && manifest.Recovery.Script is null)
        {
            throw new InvalidDataException("Script-based recovery requires a script.");
        }
    }

    private static void ValidateSteps(MigrationManifest manifest)
    {
        HashSet<MigrationPhase> phases = [];
        MigrationStep? previous = null;
        foreach (var step in manifest.Steps)
        {
            if (step is null)
            {
                throw new InvalidDataException("Migration steps must be objects.");
            }

            if (step.Order <= 0 || previous is not null && (step.Phase, step.Order).CompareTo((previous.Phase, previous.Order)) <= 0)
            {
                throw new InvalidDataException("Migration steps must be strictly ordered by phase and order.");
            }

            ValidateStep(step);
            _ = phases.Add(step.Phase);
            previous = step;
        }

        if (!phases.Contains(MigrationPhase.Verify))
        {
            throw new InvalidDataException("A migration requires a verification step.");
        }

        foreach (var omission in manifest.OmittedPhases)
        {
            if (omission is null || string.IsNullOrWhiteSpace(omission.Reason) || !phases.Add(omission.Phase))
            {
                throw new InvalidDataException("An omitted phase requires a reason and no steps.");
            }
        }

        if (phases.Count != Enum.GetValues<MigrationPhase>().Length)
        {
            throw new InvalidDataException("Every phase must have steps or an explicit omission.");
        }
    }

    private static void ValidateStep(MigrationStep step)
    {
        if (step.TimeoutSeconds <= 0 || step.LockTimeoutSeconds <= 0 || step.LockTimeoutSeconds > step.TimeoutSeconds)
        {
            throw new InvalidDataException("Step and lock timeouts must be positive and the lock timeout must fit within the step.");
        }

        if (step.Destructive && step.Phase != MigrationPhase.Contract)
        {
            throw new InvalidDataException("Destructive changes belong to the contract phase.");
        }

        if (step.Transaction == MigrationTransaction.Batched && (step.Phase != MigrationPhase.Data || !IsIdentifier(step.Checkpoint)))
        {
            throw new InvalidDataException("A batched data step requires a checkpoint identifier.");
        }

        if (step.Transaction != MigrationTransaction.Atomic && step.Verify is null)
        {
            throw new InvalidDataException("Batched and online steps require a verification script.");
        }

        if (step.Transaction == MigrationTransaction.Online
            && (step.Phase != MigrationPhase.Expand || step.Checkpoint is not null || step.OnlineIndex is null))
        {
            throw new InvalidDataException("Online execution requires a declared index on an expansion step.");
        }

        if (step.OnlineIndex is { } index)
        {
            if (step.Transaction != MigrationTransaction.Online)
            {
                throw new InvalidDataException("An online index declaration requires online transaction mode.");
            }

            index.Validate();
        }
    }

    private static bool IsIdentifier(string? value) =>
        value is { Length: > 0 and <= 80 } && value.All(static character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_');
}
