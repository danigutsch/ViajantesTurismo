using SharedKernel.IntegrationTesting;
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
    public async Task<CatalogTourDto> WaitForCatalogTour(Guid adminTourId, string identifier, CancellationToken ct)
    {
        return await Eventually.Until(
            async ct =>
            {
                var matches = await FindCatalogTours(identifier, ct);
                if (matches?.SingleOrDefault() is not { } match)
                {
                    return null;
                }

                var outcome = await catalogTours.GetTour(match.Id, includeImages: false, ct);
                if (outcome is { Kind: SharedKernel.HttpClients.HttpOutcomeKind.Succeeded, Value: { } tour })
                {
                    return tour.AdminTourId == adminTourId ? tour : null;
                }

                if (outcome.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.NotFound)
                {
                    return null;
                }

                throw new HttpRequestException("Catalog request failed.", null, outcome.StatusCode);
            },
            ProjectionTimeout,
            ct);
    }

    /// <summary>Finds Catalog rows with the scenario's unique tour identifier, returning null on a transient error.</summary>
    public async Task<IReadOnlyList<CatalogTourListItemDto>?> FindCatalogTours(string identifier, CancellationToken ct)
    {
        var matches = new List<CatalogTourListItemDto>();
        for (var page = 1; ; page++)
        {
            var outcome = await catalogTours.GetTourPage(page, CatalogTourListSort.Identifier, descending: false, ct);
            if (outcome.StatusCode == HttpStatusCode.InternalServerError)
            {
                return null;
            }

            if (outcome is not { Kind: SharedKernel.HttpClients.HttpOutcomeKind.Succeeded, Value: { } result })
            {
                throw new HttpRequestException("Catalog request failed.", null, outcome.StatusCode);
            }

            matches.AddRange(result.Items.Where(tour => string.Equals(tour.Identifier, identifier, StringComparison.Ordinal)));
            if ((long)page * CatalogTourPageDto.ItemsPerPage >= result.Total)
            {
                return matches;
            }
        }
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
            Kind: SharedKernel.HttpClients.HttpOutcomeKind.Succeeded,
            Value: { } published
        }
            ? published
            : throw new InvalidOperationException($"Catalog tour '{tour.Id}' was not found for publication.");
        var publishOutcome = await catalogTours.Publish(
            tour.Id,
            new CatalogTourPublicationRequest { ExpectedVersion = updated.Version },
            ct);
        if (publishOutcome.Kind != SharedKernel.HttpClients.HttpOutcomeKind.Succeeded)
        {
            throw new HttpRequestException("Catalog publication failed.", null, publishOutcome.StatusCode);
        }

        var reloadedOutcome = await catalogTours.GetTour(tour.Id, ct);
        return reloadedOutcome is
        {
            Kind: SharedKernel.HttpClients.HttpOutcomeKind.Succeeded,
            Value: { } reloaded
        }
            ? reloaded
            : throw new InvalidOperationException($"Catalog tour '{tour.Id}' was not found after publication.");
    }

    /// <summary>Waits for a retirement replay to confirm that Catalog has withdrawn the tour.</summary>
    public async Task WaitForRetirement(Guid adminTourId, CancellationToken ct)
    {
        var path = new Uri($"/api/v1/tours/{adminTourId:D}", UriKind.Relative);
        await Eventually.Until(
            async ct =>
            {
                using var response = await adminApi.DeleteAsync(path, ct);
                if (response.StatusCode is HttpStatusCode.Accepted)
                {
                    return null;
                }

                return response.StatusCode is HttpStatusCode.NoContent
                    ? path
                    : throw new HttpRequestException("Tour retirement replay failed.", null, response.StatusCode);
            },
            ProjectionTimeout,
            ct);
    }
}
