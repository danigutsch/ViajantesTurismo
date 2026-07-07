using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Helpers;

internal sealed class PostTransportValidationScenario(
    HttpClient adminApi,
    ICatalogToursApiClient catalogTours)
{
    private static readonly TimeSpan ProjectionTimeout = TimeSpan.FromSeconds(45);

    public async Task<GetTourDto> CreateAdminTour(string identifier, string name)
    {
        return await adminApi.CreateTour(new CreateTourOptions
        {
            Identifier = identifier,
            Name = name
        });
    }

    public async Task<CatalogTourDto> WaitForCatalogTour(Guid adminTourId, CancellationToken ct)
    {
        return await Eventually.Until(
            async probeCt =>
            {
                var tours = await catalogTours.GetTours(probeCt);
                return tours.SingleOrDefault(tour => tour.AdminTourId == adminTourId);
            },
            ProjectionTimeout,
            ct);
    }

    public async Task<CatalogTourDto> PublishCatalogTour(CatalogTourDto tour, string title, string slug, CancellationToken ct)
    {
        var published = await catalogTours.UpdatePresentation(
            tour.Id,
            new UpsertCatalogTourPresentationRequest
            {
                Title = title,
                Slug = slug,
                IsPublished = true
            },
            ct);

        return published ?? throw new InvalidOperationException($"Catalog tour '{tour.Id}' was not found for publication.");
    }
}
