namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Represents why a regenerated document revision needs a reviewer to acknowledge what changed.</summary>
[Flags]
public enum DocumentManualReviewReasonsDto
{
    /// <summary>No change needs manual review.</summary>
    None = 0,

    /// <summary>The source data version differs from the replaced revision.</summary>
    SourceChanged = 1,

    /// <summary>The template version differs from the replaced revision.</summary>
    TemplateChanged = 2
}
