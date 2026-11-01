namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Classifies why a regenerated revision needs a reviewer to acknowledge what changed.</summary>
[Flags]
public enum DocumentManualReviewReasons
{
    /// <summary>No change needs manual review.</summary>
    None = 0,

    /// <summary>The source data version differs from the replaced revision.</summary>
    SourceChanged = 1,

    /// <summary>The template version differs from the replaced revision.</summary>
    TemplateChanged = 2
}
