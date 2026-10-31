using Microsoft.AspNetCore.Mvc;
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
            .WithAdminMetadata("PlaceDocumentHold", "Places a disposition hold on the lineage of a document so purges skip it. The 201 Location is the hold collection of the document; there is no single-hold GET, so read the created hold from the response body or the list endpoint.", "Places a document hold.");

        group.MapGet("/{id:guid}/holds", ListHolds)
            .RequireAuthorization(AdminAuthorization.DocumentHold)
            .Produces<IReadOnlyList<GetDocumentHoldDto>>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithAdminMetadata("ListDocumentHolds", "Lists every hold, released or not, on the lineage of a document.", "Lists document holds.");

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
            new PlaceDocumentHoldCommand(id, basis, dto.PurposeCode, dto.AuthorityReference, dto.ReviewDueAt, dto.ExpiresAt, DocumentEndpoints.CreateAuditContext(httpContext)),
            ct);
        return result.IsFailure
            ? ToError(result.ConvertError())
            : TypedResults.Created(
                $"/api/v1/documents/{id}/holds",
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

        var result = await handler.Handle(new ReleaseDocumentHoldCommand(id, holdId, dto.ReasonCode, DocumentEndpoints.CreateAuditContext(httpContext)), ct);
        return result.IsFailure
            ? ToError(result.ConvertError())
            : TypedResults.Ok(DocumentHoldMapper.MapToGetDocumentHoldDto(result.Value, timeProvider.GetUtcNow()));
    }

    private static IResult ToError(Result result) => result.Status switch
    {
        ResultStatus.NotFound => result.ToNotFound(),
        ResultStatus.Conflict => result.ToConflict(),
        ResultStatus.Invalid => result.ToValidationProblem(),
        _ => TypedResults.Problem("The hold operation could not be completed.", statusCode: StatusCodes.Status500InternalServerError)
    };
}
