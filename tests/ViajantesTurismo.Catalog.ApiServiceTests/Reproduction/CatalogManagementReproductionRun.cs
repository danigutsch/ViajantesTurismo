using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Testing.Reproduction;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Reproduction;

/// <summary>Captures the management HTTP outcomes and current presentation from one reset.</summary>
/// <param name="TourId">The synthetic tour owned by the attempt.</param>
/// <param name="Forbidden">The Guest edit response status.</param>
/// <param name="Invalid">The invalid Admin edit response status.</param>
/// <param name="Committed">The valid Admin edit response status.</param>
/// <param name="Stale">The stale Admin edit response status.</param>
/// <param name="Read">The management read response status.</param>
/// <param name="Presentation">The tour read after the stale edit.</param>
/// <param name="ObservedRequests">Number of requests seen under the attempt's trace.</param>
/// <param name="Manifest">The approved local diagnostic values.</param>
internal sealed record CatalogManagementReproductionRun(
    Guid TourId,
    HttpStatusCode Forbidden,
    HttpStatusCode Invalid,
    HttpStatusCode Committed,
    HttpStatusCode Stale,
    HttpStatusCode Read,
    CatalogTourDto? Presentation,
    int ObservedRequests,
    ReproductionManifest Manifest);
