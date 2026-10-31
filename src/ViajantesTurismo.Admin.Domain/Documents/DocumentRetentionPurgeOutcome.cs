namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Names how a retention purge run ended.</summary>
public enum DocumentRetentionPurgeOutcome
{
    /// <summary>The run completed and removed every eligible, unheld candidate of its batch.</summary>
    Succeeded,

    /// <summary>The run was refused because hold state could not be determined, so it removed nothing.</summary>
    Refused
}
