namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>
/// Registry of the current document template versions. Bump a version whenever its template changes so
/// drafts generated from the older template are rejected at approval and finalization.
/// </summary>
public static class DocumentTemplates
{
    /// <summary>Identifier of the tour service contract template.</summary>
    public const string TourServiceContractId = "tour-service-contract";

    /// <summary>Current version of the tour service contract template.</summary>
    public const string TourServiceContractVersion = "1";

    /// <summary>Checks that a template version is the registered current version of that template.</summary>
    /// <param name="templateId">The template identifier.</param>
    /// <param name="templateVersion">The template version to check.</param>
    /// <returns><see langword="true"/> when the template is registered and the version is current.</returns>
    public static bool IsCurrent(string templateId, string templateVersion) =>
        string.Equals(templateId, TourServiceContractId, StringComparison.Ordinal) &&
        string.Equals(templateVersion, TourServiceContractVersion, StringComparison.Ordinal);
}
