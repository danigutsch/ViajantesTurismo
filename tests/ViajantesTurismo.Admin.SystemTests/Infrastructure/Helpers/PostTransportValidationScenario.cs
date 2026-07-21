using System.Net;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.Http;

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
                try
                {
                    var tours = await catalogTours.GetTours(probeCt);
                    return tours.SingleOrDefault(tour => tour.AdminTourId == adminTourId);
                }
                catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.InternalServerError)
                {
                    return null;
                }
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
                Summary = $"Discover {title} by bicycle.",
                ExpectedVersion = tour.Version
            },
            ct);
        var updated = published ?? throw new InvalidOperationException($"Catalog tour '{tour.Id}' was not found for publication.");
        await catalogTours.Publish(
            tour.Id,
            new CatalogTourPublicationRequest { ExpectedVersion = updated.Version },
            ct);

        return await catalogTours.GetTour(tour.Id, ct)
            ?? throw new InvalidOperationException($"Catalog tour '{tour.Id}' was not found after publication.");
    }
}
