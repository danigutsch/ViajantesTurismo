namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Configures how document generation captures Branding.</summary>
public sealed class DocumentBrandingOptions
{
    /// <summary>The configuration section that holds these options.</summary>
    public const string SectionName = "Documents:Branding";

    /// <summary>Gets or sets the Branding capture source. Defaults to HTTP until the projection is shadow-verified.</summary>
    public DocumentBrandingSourceMode Source { get; set; } = DocumentBrandingSourceMode.Http;
}
