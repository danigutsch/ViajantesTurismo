using Microsoft.AspNetCore.Mvc;
using PandaBear.AspNetCore;
using PandaBear.DispositionHold;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiService.Documents;

/// <summary>Defines the document.hold endpoints that place, release and list disposition holds on a document lineage.</summary>
internal static class DocumentHoldEndpoints
{
    /// <summary>Maps the hold endpoints under the documents group; every route requires the document.hold permission.</summary>
    public static void MapDocumentHoldEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapDocumentsGroup();

        group.MapPost("/{id:guid}/holds", PlaceHold)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.DocumentHold)
            .Produces<GetDocumentHoldDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithAdminMetadata("PlaceDocumentHold", "Places a disposition hold on the lineage of a document so purges skip it. The 201 Location is the created hold.", "Places a document hold.");

        group.MapGet("/{id:guid}/holds", ListHolds)
            .RequireAuthorization(AdminAuthorization.DocumentHold)
            .Produces<IReadOnlyList<GetDocumentHoldDto>>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithAdminMetadata("ListDocumentHolds", "Lists every hold, released or not, on the lineage of a document.", "Lists document holds.");

        group.MapGet("/{id:guid}/holds/{holdId:guid}", GetHold)
            .RequireAuthorization(AdminAuthorization.DocumentHold)
            .Produces<GetDocumentHoldDto>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithAdminMetadata("GetDocumentHold", "Reads one hold, released or not, on the lineage of a document. An unknown hold and a hold of another document are both not found.", "Gets a document hold.");

        group.MapGet("/holds", ListHoldOverview)
            .RequireAuthorization(AdminAuthorization.DocumentHold)
            .Produces<GetDocumentHoldOverviewPageDto>()
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .WithAdminMetadata("ListDocumentHoldOverview", "Lists holds across documents, oldest placement first, one bounded page at a time. Filters: state (Active is every unreleased hold, OverdueReview is unreleased with a review date before now, Released), basis, and releasedSince (an ISO 8601 round-trip instant, inclusive). Page defaults to 1 and pageSize to 25, at most 100.", "Lists holds across documents.");

        group.MapPost("/{id:guid}/holds/{holdId:guid}/release", ReleaseHold)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.DocumentHold)
            .Produces<GetDocumentHoldDto>()
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .WithAdminMetadata("ReleaseDocumentHold", "Releases a disposition hold, recording the releasing actor and reason code.", "Releases a document hold.");
    }

    private static async Task<IResult> PlaceHold(
        [FromRoute] Guid id,
        [FromBody] CreateDocumentHoldDto dto,
        [FromServices] PlaceDocumentHoldCommandHandler handler,
        [FromServices] TimeProvider timeProvider,
        HttpContext httpContext,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (!Enum.GetNames<HoldBasis>().Contains(dto.Basis, StringComparer.Ordinal)
            || !Enum.TryParse<HoldBasis>(dto.Basis, ignoreCase: false, out var basis))
        {
            return ToError(DocumentHoldErrors.InvalidField("basis"));
        }

        var result = await handler.Handle(
            new PlaceDocumentHoldCommand(id, basis, dto.PurposeCode, dto.AuthorityReference, dto.ReviewDueAt, dto.ExpiresAt, GetActorId(httpContext)),
            ct);
        return result.IsFailure
            ? ToError(result.ConvertError())
            : TypedResults.Created(
                $"/api/v1/documents/{id}/holds/{result.Value.Id}",
                DocumentHoldMapper.MapToGetDocumentHoldDto(result.Value, timeProvider.GetUtcNow()));
    }

    private static async Task<IResult> ListHolds(
        [FromRoute] Guid id,
        [FromServices] ListDocumentHoldsQueryHandler handler,
        CancellationToken ct)
    {
        var result = await handler.Handle(new ListDocumentHoldsQuery(id), ct);
        return result.IsFailure ? ToError(result.ConvertError()) : TypedResults.Ok(result.Value);
    }

    private static async Task<IResult> GetHold(
        [FromRoute] Guid id,
        [FromRoute] Guid holdId,
        [FromServices] GetDocumentHoldQueryHandler handler,
        CancellationToken ct)
    {
        var result = await handler.Handle(new GetDocumentHoldQuery(id, holdId), ct);
        return result.IsFailure ? ToError(result.ConvertError()) : TypedResults.Ok(result.Value);
    }

    private static async Task<IResult> ListHoldOverview(
        [FromQuery] string? state,
        [FromQuery] string? basis,
        [FromQuery] string? releasedSince,
        [FromQuery] string? page,
        [FromQuery] string? pageSize,
        [FromServices] ListDocumentHoldOverviewQueryHandler handler,
        CancellationToken ct)
    {
        var query = DocumentHoldOverviewQueryParser.Parse(state, basis, releasedSince, page, pageSize);
        if (query.IsFailure)
        {
            return ToError(query.ConvertError());
        }

        var result = await handler.Handle(query.Value, ct);
        return result.IsFailure ? ToError(result.ConvertError()) : TypedResults.Ok(result.Value);
    }

    private static async Task<IResult> ReleaseHold(
        [FromRoute] Guid id,
        [FromRoute] Guid holdId,
        [FromBody] ReleaseDocumentHoldDto dto,
        [FromServices] ReleaseDocumentHoldCommandHandler handler,
        [FromServices] TimeProvider timeProvider,
        HttpContext httpContext,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var result = await handler.Handle(new ReleaseDocumentHoldCommand(id, holdId, dto.ReasonCode, GetActorId(httpContext)), ct);
        return result.IsFailure
            ? ToError(result.ConvertError())
            : TypedResults.Ok(DocumentHoldMapper.MapToGetDocumentHoldDto(result.Value, timeProvider.GetUtcNow()));
    }

    private static string GetActorId(HttpContext httpContext)
    {
        var actorId = httpContext.User.GetSubjectId();
        return string.IsNullOrWhiteSpace(actorId)
            ? throw new InvalidOperationException("Hold operations require an authenticated subject identifier.")
            : actorId;
    }

    private static IResult ToError(Result result) => result.Status switch
    {
        ResultStatus.NotFound => result.ToNotFound(),
        ResultStatus.Conflict => result.ToConflict(),
        ResultStatus.Invalid => result.ToValidationProblem(),
        _ => TypedResults.Problem("The hold operation could not be completed.", statusCode: StatusCodes.Status500InternalServerError)
    };
}
