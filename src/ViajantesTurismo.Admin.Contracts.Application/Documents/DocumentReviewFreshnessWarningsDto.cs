namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Bounded freshness warnings an authorized reviewer can see before submitting a decision.</summary>
[Flags]
public enum DocumentReviewFreshnessWarningsDto
{
    /// <summary>No freshness warning applies.</summary>
    None = 0,

    /// <summary>Source data has changed since the revision was generated.</summary>
    SourceChanged = 1,

    /// <summary>The revision was generated using an outdated template.</summary>
    TemplateChanged = 2,

    /// <summary>The source booking or tour is unavailable.</summary>
    SourceUnavailable = 4
}
