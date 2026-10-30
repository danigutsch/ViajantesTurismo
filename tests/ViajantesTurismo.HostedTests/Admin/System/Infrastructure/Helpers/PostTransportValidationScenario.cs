using ViajantesTurismo.Admin.Testing.Integration.Helpers;
using System.Net;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.Http;

namespace ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Helpers;

/// <summary>Drives the Admin to Catalog tour transport validation scenario through both APIs.</summary>
/// <param name="adminApi">The authenticated Admin API client.</param>
/// <param name="catalogTours">The Catalog tours API client.</param>
internal sealed class PostTransportValidationScenario(
    HttpClient adminApi,
    ICatalogToursApiClient catalogTours)
{
    private static readonly TimeSpan ProjectionTimeout = TimeSpan.FromSeconds(45);

    /// <summary>Creates an admin tour with the given identifier and name.</summary>
    public async Task<GetTourDto> CreateAdminTour(string identifier, string name)
    {
        return await adminApi.CreateTour(new CreateTourOptions
        {
            Identifier = identifier,
            Name = name
        });
    }

    /// <summary>Waits until the admin tour is projected into the Catalog, tolerating transient server errors.</summary>
    public async Task<CatalogTourDto> WaitForCatalogTour(Guid adminTourId, CancellationToken ct)
    {
        return await Eventually.Until(
            async ct =>
            {
                var outcome = await catalogTours.GetTours(ct);
                if (outcome is { Kind: PandaBear.HttpClients.HttpOutcomeKind.Succeeded, Value: { } tours })
                {
                    return tours.SingleOrDefault(tour => tour.AdminTourId == adminTourId);
                }

                if (outcome.StatusCode == HttpStatusCode.InternalServerError)
                {
                    return null;
                }

                throw new HttpRequestException("Catalog request failed.", null, outcome.StatusCode);
            },
            ProjectionTimeout,
            ct);
    }

    /// <summary>Sets the Catalog presentation of the tour, publishes it and returns the reloaded published tour.</summary>
    public async Task<CatalogTourDto> PublishCatalogTour(CatalogTourDto tour, string title, string slug, CancellationToken ct)
    {
        var publishedOutcome = await catalogTours.UpdatePresentation(
            tour.Id,
            new UpsertCatalogTourPresentationRequest
            {
                Title = title,
                Slug = slug,
                Summary = $"Discover {title} by bicycle.",
                ExpectedVersion = tour.Version
            },
            ct);
        var updated = publishedOutcome is
        {
            Kind: PandaBear.HttpClients.HttpOutcomeKind.Succeeded,
            Value: { } published
        }
            ? published
            : throw new InvalidOperationException($"Catalog tour '{tour.Id}' was not found for publication.");
        var publishOutcome = await catalogTours.Publish(
            tour.Id,
            new CatalogTourPublicationRequest { ExpectedVersion = updated.Version },
            ct);
        if (publishOutcome.Kind != PandaBear.HttpClients.HttpOutcomeKind.Succeeded)
        {
            throw new HttpRequestException("Catalog publication failed.", null, publishOutcome.StatusCode);
        }

        var reloadedOutcome = await catalogTours.GetTour(tour.Id, ct);
        return reloadedOutcome is
        {
            Kind: PandaBear.HttpClients.HttpOutcomeKind.Succeeded,
            Value: { } reloaded
        }
            ? reloaded
            : throw new InvalidOperationException($"Catalog tour '{tour.Id}' was not found after publication.");
    }
}
