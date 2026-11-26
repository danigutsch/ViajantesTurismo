using System.Globalization;
using System.Net;
using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Reads aggregate messaging health through the authenticated Admin API.</summary>
public sealed class MessagingHealthApiClient(HttpClient httpClient) : IMessagingHealthApiClient
{
    private const string MessagingRoute = "/api/v1/operations/messaging";
    private const string InvalidPageMessage = "Messaging response contained an invalid page.";

    /// <inheritdoc />
    public async Task<HttpOutcome<GetMessagingHealthDto>> ReadMessagingHealth(CancellationToken ct)
    {
        var json = MessagingHealthApiClientJsonContext.Default;
        return await HttpOutcomeMapper.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, new Uri(MessagingRoute, UriKind.Relative)),
            HttpStatusCode.OK,
            json.GetMessagingHealthDto,
            json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<GetCatalogDeliveryPageDto>> ReadCatalogDelivery(CatalogDeliveryFilters filters, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filters);

        var query = BuildDeliveryQuery(
            ContractConstants.DefaultCatalogDeliveryPageSize, filters.EventId, filters.EventType, filters.RecordedSince,
            filters.AfterRecordedAt, filters.AfterStorageId);
        var json = MessagingHealthApiClientJsonContext.Default;
        var outcome = await HttpOutcomeMapper.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, new Uri($"{MessagingRoute}/catalog-delivery{query}", UriKind.Relative)),
            HttpStatusCode.OK,
            json.GetCatalogDeliveryPageDto,
            json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
        return outcome.Kind == HttpOutcomeKind.Succeeded
            && (outcome.Value is not { } page || !IsCatalogDeliveryPage(page, filters))
            ? HttpOutcomeMapper.Status<GetCatalogDeliveryPageDto>(HttpOutcomeKind.MalformedBody, HttpStatusCode.OK, InvalidPageMessage)
            : outcome;
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<GetOutboxDeliveryPageDto>> ReadOutboxDelivery(OutboxDeliveryFilters filters, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filters);

        var query = BuildDeliveryQuery(
            ContractConstants.DefaultOutboxDeliveryPageSize, filters.EventId, filters.EventType, filters.RecordedSince,
            filters.AfterRecordedAt, filters.AfterStorageId);
        if (filters.IncludePublished)
        {
            query += "&includePublished=true";
        }
        var json = MessagingHealthApiClientJsonContext.Default;
        var outcome = await HttpOutcomeMapper.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, new Uri($"{MessagingRoute}/outbox{query}", UriKind.Relative)),
            HttpStatusCode.OK,
            json.GetOutboxDeliveryPageDto,
            json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
        return outcome.Kind == HttpOutcomeKind.Succeeded
            && (outcome.Value is not { } page || !IsOutboxDeliveryPage(page, filters))
            ? HttpOutcomeMapper.Status<GetOutboxDeliveryPageDto>(HttpOutcomeKind.MalformedBody, HttpStatusCode.OK, InvalidPageMessage)
            : outcome;
    }

    private static bool IsCatalogDeliveryPage(GetCatalogDeliveryPageDto page, CatalogDeliveryFilters filters) =>
        IsCompletePage(page.Rows, ContractConstants.DefaultCatalogDeliveryPageSize, page.NextRecordedAt, page.NextStorageId,
            (filters.AfterRecordedAt, filters.AfterStorageId),
            static row => (row.RecordedAt, row.StorageId))
        && page.Rows.All(row => MatchesSearch(row.EventId, row.EventType, row.RecordedAt, filters.EventId, filters.EventType, filters.RecordedSince));

    private static bool IsOutboxDeliveryPage(GetOutboxDeliveryPageDto page, OutboxDeliveryFilters filters)
    {
        var limit = filters.IncludePublished ? 1 : ContractConstants.DefaultOutboxDeliveryPageSize;
        return (page.IncludesPublished ?? false) == filters.IncludePublished
            && (!filters.IncludePublished || !page.NextRecordedAt.HasValue)
            && IsCompletePage(page.Rows, limit, page.NextRecordedAt, page.NextStorageId,
                (filters.AfterRecordedAt, filters.AfterStorageId),
                static row => (row.RecordedAt, row.StorageId))
            && page.Rows.All(row => MatchesSearch(row.EventId, row.EventType, row.RecordedAt, filters.EventId, filters.EventType, filters.RecordedSince));
    }

    private static bool IsCompletePage<TRow>(
        IReadOnlyList<TRow>? rows, int limit, DateTimeOffset? nextRecordedAt, Guid? nextStorageId,
        (DateTimeOffset? RecordedAt, Guid? StorageId) submittedCursor,
        Func<TRow, (DateTimeOffset RecordedAt, Guid StorageId)> position) where TRow : class =>
        rows is not null && rows.Count <= limit && rows.All(static row => row is not null)
        && nextRecordedAt.HasValue == nextStorageId.HasValue && nextStorageId != Guid.Empty
        && (nextRecordedAt is null || (rows is [.., var last]
            && position(last) == (nextRecordedAt, nextStorageId)
            && (nextRecordedAt, nextStorageId) != submittedCursor));

    private static bool MatchesSearch(
        string eventId, string eventType, DateTimeOffset recordedAt,
        string? requestedEventId, string? requestedEventType, DateTimeOffset? recordedSince) =>
        (string.IsNullOrWhiteSpace(requestedEventId) || string.Equals(eventId, requestedEventId, StringComparison.Ordinal))
        && (string.IsNullOrWhiteSpace(requestedEventType) || string.Equals(eventType, requestedEventType, StringComparison.Ordinal))
        && (recordedSince is not { } since || recordedAt >= since);

    private static string BuildDeliveryQuery(
        int pageSize, string? eventId, string? eventType, DateTimeOffset? recordedSince,
        DateTimeOffset? afterRecordedAt, Guid? afterStorageId)
    {
        List<string> parts = [];
        AddText(parts, "limit", pageSize.ToString(CultureInfo.InvariantCulture));
        AddText(parts, "eventId", eventId);
        AddText(parts, "eventType", eventType);
        AddText(parts, "recordedSince", recordedSince?.ToString("O", CultureInfo.InvariantCulture));
        AddText(parts, "afterRecordedAt", afterRecordedAt?.ToString("O", CultureInfo.InvariantCulture));
        AddText(parts, "afterStorageId", afterStorageId?.ToString("D"));
        return "?" + string.Join('&', parts);
    }

    private static void AddText(List<string> parts, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add($"{name}={Uri.EscapeDataString(value)}");
        }
    }
}
