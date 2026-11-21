using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Testing.Reproduction;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Reproduction;

/// <summary>Captures the anonymous localized read from one reset.</summary>
/// <param name="ContentKey">The synthetic published content key owned by the attempt.</param>
/// <param name="Status">The public response status.</param>
/// <param name="Content">The variant returned to the visitor.</param>
/// <param name="ObservedRequests">Number of requests seen under the attempt's trace.</param>
/// <param name="Manifest">The approved local diagnostic values.</param>
internal sealed record CatalogLocalizedReadReproductionRun(
    string ContentKey,
    HttpStatusCode Status,
    PublicContentVariantDto? Content,
    int ObservedRequests,
    ReproductionManifest Manifest);
