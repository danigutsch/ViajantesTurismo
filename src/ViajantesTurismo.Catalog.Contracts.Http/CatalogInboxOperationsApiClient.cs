using System.Globalization;
using System.Net;
using System.Text.Json;
using SharedKernel.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>Reads the Admin-only inbox evidence from the Catalog API.</summary>
public sealed class CatalogInboxOperationsApiClient(HttpClient httpClient) : ICatalogInboxOperationsApiClient
{
    private const string InboxRoute = "/api/v1/catalog/operations/messaging/inbox";

    private static readonly CatalogToursApiClientJsonContext InboxJson = new(
        new JsonSerializerOptions(JsonSerializerDefaults.Web) { RespectRequiredConstructorParameters = true });

    /// <inheritdoc />
    public Task<HttpOutcome<GetCatalogInboxHealthDto>> ReadHealth(CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(InboxRoute, UriKind.Relative));
        return HttpOutcomeMapper.Send(httpClient, request, HttpStatusCode.OK, InboxJson.GetCatalogInboxHealthDto,
            CatalogToursApiClientJsonContext.Default.ContractValidationProblemDto, ct);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<GetCatalogInboxDuplicatePageDto>> ReadPage(CatalogInboxDuplicateFilters filters, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filters);

        var path = $"{InboxRoute}/duplicates?limit={filters.Limit.ToString(CultureInfo.InvariantCulture)}";
        if (filters.EventId is { } eventId)
        {
            path += $"&eventId={eventId:D}";
        }

        if (filters.ObservedSince is { } since)
        {
            var observedSince = Uri.EscapeDataString(since.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            path += $"&observedSince={observedSince}";
        }

        if (filters.AfterObservedAt is { } afterObservedAt && filters.AfterEventId is { } afterEventId)
        {
            var observedAt = Uri.EscapeDataString(afterObservedAt.ToString("O", CultureInfo.InvariantCulture));
            path += $"&afterObservedAt={observedAt}&afterEventId={afterEventId:D}";
        }

        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        return await HttpOutcomeMapper.Send(httpClient, request, HttpStatusCode.OK, InboxJson.GetCatalogInboxDuplicatePageDto,
            CatalogToursApiClientJsonContext.Default.ContractValidationProblemDto,
            validateValue: page => page.Rows is { } rows
                && rows.Count <= filters.Limit
                && rows.All(row => row is not null
                    && (filters.EventId is not { } requestedEventId || row.EventId == requestedEventId)
                    && (filters.ObservedSince is not { } sinceBound || row.LastObservedAt >= sinceBound))
                && page.NextObservedAt.HasValue == page.NextEventId.HasValue
                && page.NextEventId != Guid.Empty
                && (page.NextObservedAt is null || (rows is [.., var last]
                    && (last.LastObservedAt, last.EventId) == (page.NextObservedAt, page.NextEventId)
                    && (page.NextObservedAt, page.NextEventId) != (filters.AfterObservedAt, filters.AfterEventId))),
            ct).ConfigureAwait(false);
    }
}
