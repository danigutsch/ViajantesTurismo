using SharedKernel.DocumentRendering;
using ViajantesTurismo.Admin.Domain.Documents;
using RenderField = SharedKernel.DocumentRendering.DocumentField;
using RenderPrivacy = SharedKernel.DocumentRendering.DocumentPrivacyClassification;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>
/// Maps the travel-contract draft to the reusable deterministic HTML renderer.
/// </summary>
internal static class DocumentArtifactRenderer
{
    public static byte[] Render(DocumentDraft document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var fields = document.Fields
            .OrderBy(field => field.FieldId, StringComparer.Ordinal)
            .Select(field => new RenderField(field.Label, field.RenderedValue, MapPrivacy(field.PrivacyClassification)))
            .ToArray();
        var request = new DocumentRenderRequest(
            "en",
            "Tour service contract",
            [new DocumentSection("Contract details", fields)],
            new DocumentBrandingSnapshot(
                document.BrandingVersion,
                document.BrandingName,
                document.BrandingLogoUri));

        return new HtmlDocumentRenderer().Render(request);
    }

    private static RenderPrivacy MapPrivacy(
        Domain.Documents.DocumentPrivacyClassification classification) => classification switch
        {
            Domain.Documents.DocumentPrivacyClassification.Public => RenderPrivacy.Public,
            Domain.Documents.DocumentPrivacyClassification.Operational => RenderPrivacy.Operational,
            Domain.Documents.DocumentPrivacyClassification.PersonalData => RenderPrivacy.PersonalData,
            Domain.Documents.DocumentPrivacyClassification.SensitivePersonalData => RenderPrivacy.SensitivePersonalData,
            _ => throw new InvalidOperationException($"Unsupported document privacy classification: {classification}."),
        };
}
