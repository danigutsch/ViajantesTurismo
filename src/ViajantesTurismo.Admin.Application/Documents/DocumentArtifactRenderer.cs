using SharedKernel.DocumentRendering;
using ViajantesTurismo.Admin.Domain.Documents;
using DomainPrivacy = ViajantesTurismo.Admin.Domain.Documents.DocumentPrivacyClassification;
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

        RenderField[] fields = [
            .. document.Fields
                .OrderBy(field => field.SortOrder)
                .Select(field => new RenderField(field.Label, field.RenderedValue, MapPrivacy(field.PrivacyClassification))),
        ];
        var request = new DocumentRenderRequest(
            "en",
            "Tour service contract",
            [new DocumentSection("Contract details", fields)],
            new DocumentBrandingSnapshot(
                document.BrandingVersion,
                document.BrandingName,
                document.BrandingLogoUri is { IsAbsoluteUri: false } logoUri ? logoUri : null,
                document.BrandingPrimaryColor,
                document.BrandingAccentColor,
                document.BrandingBackgroundColor,
                document.BrandingTextColor,
                document.BrandingHeadingFontFamily,
                document.BrandingBodyFontFamily,
                document.BrandingFooterText));

        return new HtmlDocumentRenderer().Render(request);
    }

    private static RenderPrivacy MapPrivacy(DomainPrivacy classification) => classification switch
    {
        DomainPrivacy.Public => RenderPrivacy.Public,
        DomainPrivacy.Operational => RenderPrivacy.Operational,
        DomainPrivacy.PersonalData => RenderPrivacy.PersonalData,
        DomainPrivacy.SensitivePersonalData => RenderPrivacy.SensitivePersonalData,
        _ => throw new InvalidOperationException($"Unsupported document privacy classification: {classification}."),
    };
}
