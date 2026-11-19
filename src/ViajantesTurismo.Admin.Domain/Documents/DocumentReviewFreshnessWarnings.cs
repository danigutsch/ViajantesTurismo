namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Identifies current freshness warnings for a contract revision awaiting a review decision.</summary>
[Flags]
public enum DocumentReviewFreshnessWarnings
{
    /// <summary>No freshness warning applies.</summary>
    None = 0,

    /// <summary>The current source version differs from the captured version.</summary>
    SourceChanged = 1,

    /// <summary>The template version is no longer current.</summary>
    TemplateChanged = 2,

    /// <summary>The current source version is unavailable.</summary>
    SourceUnavailable = 4
}
