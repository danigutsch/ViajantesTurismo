namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Defines domain limits for immutable document audit records.</summary>
public static class DocumentAuditLimits
{
    /// <summary>Maximum length of an opaque actor identifier.</summary>
    public const int MaxActorIdLength = 128;

    /// <summary>Maximum length of a server-generated correlation identifier.</summary>
    public const int MaxCorrelationIdLength = 128;

    /// <summary>Maximum length of a hold basis name recorded as audit evidence.</summary>
    public const int MaxHoldBasisLength = 64;

    /// <summary>Number of months audit records remain immutable and retained.</summary>
    public const int RetentionMonths = 24;
}
