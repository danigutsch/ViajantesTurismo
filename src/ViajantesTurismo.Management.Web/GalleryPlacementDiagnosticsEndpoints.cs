using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.AspNetCore.Authentication.Bff;
using SharedKernel.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Management.Web;

/// <summary>Downloads allowlisted diagnostics for one Gallery placement.</summary>
internal static class GalleryPlacementDiagnosticsEndpoints
{
    /// <summary>Registers the Admin-only export within the private operations response policy.</summary>
    internal static void MapGalleryPlacementDiagnostics(this IEndpointRouteBuilder app)
    {
        app.MapGet("/operations/gallery/{catalogTourId:guid}/placements/{sequence:long}/diagnostics", Download)
            .WithName("DownloadGalleryPlacementDiagnostics")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
    }

    /// <summary>Gets the download path for the displayed tour and placement sequence.</summary>
    internal static string DownloadPath(Guid catalogTourId, long sequence) =>
        FormattableString.Invariant($"/operations/gallery/{catalogTourId:D}/placements/{sequence}/diagnostics");

    private static async Task<IResult> Download(
        Guid catalogTourId,
        long sequence,
        [FromServices] ICatalogGalleryOperationsApiClient galleryApi,
        CancellationToken ct)
    {
        if (catalogTourId == Guid.Empty || sequence <= 0)
        {
            return TypedResults.BadRequest();
        }

        HttpOutcome<GetCatalogGalleryPlacementStatusDto> outcome;
        try
        {
            outcome = await galleryApi.Read(catalogTourId, sequence, ct);
        }
        catch (UserTokenUnavailableException)
        {
            return TypedResults.Unauthorized();
        }

        return outcome.Kind switch
        {
            HttpOutcomeKind.Succeeded when outcome.Value is { } placement => CreateDownload(placement),
            HttpOutcomeKind.NotFound => TypedResults.NotFound(),
            HttpOutcomeKind.Unauthorized => TypedResults.Unauthorized(),
            HttpOutcomeKind.Forbidden => TypedResults.StatusCode(StatusCodes.Status403Forbidden),
            HttpOutcomeKind.TimedOut => TypedResults.StatusCode(StatusCodes.Status504GatewayTimeout),
            _ => TypedResults.StatusCode(StatusCodes.Status502BadGateway),
        };
    }

    private static FileContentHttpResult CreateDownload(GetCatalogGalleryPlacementStatusDto placement)
    {
        var export = new
        {
            placement.CatalogTourId,
            placement.Sequence,
            placement.IntentId,
            Status = placement.Status.ToString(),
            placement.Revision,
            placement.AcceptedAt,
            placement.TerminalAt,
            placement.TerminalEventId,
            placement.CausationEventId,
            placement.TriggeringWakeEventId,
            OriginalWake = ExportWake(placement.OriginalWake),
            TriggeringWake = placement.CausationEventId.HasValue ? ExportWake(placement.TriggeringWake) : null,
            RelatedPendingWakes = (placement.RelatedPendingWakes ?? []).Select(static wake => new
            {
                wake.EventId,
                wake.RecordedAt,
                wake.FailedAttempts,
                wake.LastAttemptAt,
                wake.NextAttemptAt,
            }),
            Timeline = BuildTimeline(placement),
        };
        var content = JsonSerializer.SerializeToUtf8Bytes(export, JsonSerializerOptions.Web);
        return TypedResults.File(content, "application/json", $"gallery-placement-{placement.IntentId:N}.json");
    }

    private static object? ExportWake(CatalogGalleryOriginalWakeDeliveryDto? wake) => wake is null ? null : new
    {
        wake.RecordedAt,
        wake.PublishedAt,
        wake.FailedAttempts,
        wake.LastAttemptAt,
        wake.NextAttemptAt,
    };

    private static TimelineEntry[] BuildTimeline(GetCatalogGalleryPlacementStatusDto placement)
    {
        List<TimelineEntry> entries = [];
        Add("PlacementAccepted", placement.AcceptedAt, eventId: null);
        Add("PlacementTerminal", placement.TerminalAt, placement.TerminalEventId);
        if (placement.Status == CatalogGalleryPlacementStatusDto.ClosedByRetirement && placement.Retirement is { } retirement)
        {
            Add("CatalogWithdrawalRecorded", retirement.WithdrawnAt, retirement.RetirementId);
        }

        if (placement.OriginalWake is { } original)
        {
            AddWake(placement.IntentId, original.RecordedAt, original.LastAttemptAt, original.PublishedAt);
        }
        if (placement.CausationEventId is { } cause && placement.TriggeringWake is { } trigger)
        {
            AddWake(cause, trigger.RecordedAt, trigger.LastAttemptAt, trigger.PublishedAt);
        }
        foreach (var pending in placement.RelatedPendingWakes ?? [])
        {
            AddWake(pending.EventId, pending.RecordedAt, pending.LastAttemptAt, publishedAt: null);
        }

        return [.. entries.Distinct().OrderBy(static entry => entry.OccurredAtUtc)
            .ThenBy(static entry => entry.Kind, StringComparer.Ordinal)
            .ThenBy(static entry => entry.EventId)];

        void AddWake(Guid eventId, DateTimeOffset recordedAt, DateTimeOffset? lastAttemptAt, DateTimeOffset? publishedAt)
        {
            Add("WakeQueued", recordedAt, eventId);
            Add("WakeLastAttemptRecorded", lastAttemptAt, eventId);
            Add("WakePublicationRecorded", publishedAt, eventId);
        }

        void Add(string kind, DateTimeOffset? occurredAt, Guid? eventId)
        {
            if (occurredAt is { } timestamp)
            {
                entries.Add(new TimelineEntry(kind, timestamp.ToUniversalTime(), eventId));
            }
        }
    }

    private sealed record TimelineEntry(string Kind, DateTimeOffset OccurredAtUtc, Guid? EventId);
}
