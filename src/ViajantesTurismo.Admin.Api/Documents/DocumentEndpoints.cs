using SharedKernel.AspNetCore.Authentication;
using SharedKernel.Idempotency.AspNetCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.Documents;

namespace ViajantesTurismo.Admin.Api.Documents;

/// <summary>Defines Admin-only endpoints for generated document lifecycle operations.</summary>
internal static class DocumentEndpoints
{
    /// <summary>Maps generated-document endpoints to the Admin API.</summary>
    public static void MapDocumentEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var documentsGroup = app.MapDocumentsGroup().ObserveDocumentOperations();

        documentsGroup.MapPost("/bookings/{bookingId:guid}/contract-drafts", GenerateContractDraft)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.DocumentManage)
            .Produces<GetDocumentDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .WithAdminMetadata("GenerateDocumentContractDraft", "Generates the server-selected contract draft for an eligible booking.", "Generates a contract draft.");

        documentsGroup.MapGet("/{id:guid}", GetDocumentById)
            .RequireAuthorization(AdminAuthorization.DocumentManage)
            .Produces<GetDocumentDto>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .WithAdminMetadata("GetDocumentById", "Retrieves one generated document revision.", "Retrieves a document.");

        documentsGroup.MapPost("/{id:guid}/review", BeginReview)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.DocumentManage)
            .Produces<GetDocumentDto>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .WithAdminMetadata("BeginDocumentReview", "Starts or resumes staff review of a document draft.", "Starts document review.");

        documentsGroup.MapPost("/{id:guid}/changes-requested", RequestChanges)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.DocumentManage)
            .Produces<GetDocumentDto>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .WithAdminMetadata("RequestDocumentChanges", "Records requested changes for a document draft.", "Requests document changes.");

        documentsGroup.MapPost("/{id:guid}/changes-acknowledged", AcknowledgeChanges)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.DocumentManage)
            .Produces<GetDocumentDto>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .WithAdminMetadata("AcknowledgeDocumentChanges", "Acknowledges the source or template changes of a regenerated document draft so it can be approved.", "Acknowledges document changes.");

        documentsGroup.MapPatch("/{id:guid}/fields/{fieldId}", UpdateField)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.DocumentManage)
            .Produces<GetDocumentDto>()
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .WithAdminMetadata("UpdateDocumentField", "Updates one staff-editable document field.", "Updates a document field.");

        documentsGroup.MapPost("/{id:guid}/approve", Approve)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.DocumentManage)
            .Produces<GetDocumentDto>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .WithAdminMetadata("ApproveDocument", "Approves a document draft for finalization.", "Approves a document.");

        documentsGroup.MapPost("/{id:guid}/finalize", FinalizeDocument)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.DocumentManage)
            .Produces<GetDocumentDto>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .WithAdminMetadata("FinalizeDocument", "Finalizes and seals a document artifact.", "Finalizes a document.");

        documentsGroup.MapPost("/{id:guid}/regenerate", Regenerate)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.DocumentManage)
            .Produces<GetDocumentDto>()
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .WithAdminMetadata("RegenerateDocument", "Creates a replacement document revision from current source data.", "Regenerates a document.");

        documentsGroup.MapPost("/{id:guid}/void", Void)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.DocumentManage)
            .Produces<GetDocumentDto>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .WithAdminMetadata("VoidDocument", "Voids a document using the server-defined administrative reason code.", "Voids a document.");

        documentsGroup.MapGet("/{id:guid}/download", DownloadFinalizedArtifact)
            .RequireAuthorization(AdminAuthorization.DocumentManage)
            .Produces<Stream>(StatusCodes.Status200OK, contentType: "text/html")
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .WithAdminMetadata("DownloadDocumentArtifact", "Downloads a finalized document artifact through a mediated response.", "Downloads a document artifact.");
    }

    [ObserveDocumentOperation(DocumentOperation.Generate)]
    private static async Task<IResult> GenerateContractDraft(
        [FromRoute] Guid bookingId,
        [Required, FromHeader(Name = IdempotencyKeyHeader.Name)] string? idempotencyKeyHeader,
        [FromServices] ISender sender,
        [FromServices] IDocumentQueryService queryService,
        [FromServices] IServiceScopeFactory scopeFactory,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var keyResult = IdempotencyKeyHeader.Create(idempotencyKeyHeader);
        if (keyResult.IsFailure)
        {
            return ToError(keyResult.ConvertError());
        }

        var auditContext = CreateAuditContext(httpContext);
        var result = await ExecuteDocumentCommand(() => sender.Send(
            new GenerateContractDraftCommand(
                bookingId,
                auditContext,
                keyResult.Value),
            ct).AsTask(),
            new DocumentCommandAuditMetadata(auditContext, DocumentAuditOperation.Generate, null, bookingId),
            scopeFactory,
            ct);
        if (result.IsFailure)
        {
            return ToError(result.ConvertError());
        }

        var document = await queryService.GetById(result.Value, ct);
        return document is null
            ? ToError(DocumentErrors.DocumentNotFound())
            : TypedResults.Created($"/api/v1/documents/{document.Id}", document);
    }

    [ObserveDocumentOperation(DocumentOperation.Preview)]
    private static async Task<IResult> GetDocumentById(
        [FromRoute] Guid id,
        [FromServices] IDocumentQueryService queryService,
        [FromServices] DocumentAuditWriter documentAuditWriter,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var auditContext = CreateAuditContext(httpContext);
        var document = await queryService.GetById(id, ct);
        if (document is null)
        {
            var auditResult = await documentAuditWriter.Add(
                auditContext,
                DocumentAuditOperation.Read,
                id,
                null,
                null,
                DocumentAuditOutcome.Rejected,
                DocumentAuditReasonCode.DocumentNotFound,
                ct);
            return auditResult.IsFailure ? ToError(auditResult) : ToError(DocumentErrors.DocumentNotFound());
        }

        var successfulAudit = await documentAuditWriter.Add(
            auditContext,
            DocumentAuditOperation.Read,
            document.Id,
            document.BookingId,
            document.Revision,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.None,
            ct);
        return successfulAudit.IsFailure ? ToError(successfulAudit) : TypedResults.Ok(document);
    }

    [ObserveDocumentOperation(DocumentOperation.Review)]
    private static async Task<IResult> BeginReview(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        [FromServices] IDocumentQueryService queryService,
        [FromServices] IServiceScopeFactory scopeFactory,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var auditContext = CreateAuditContext(httpContext);
        return await GetDocumentAfterCommand(
            await ExecuteDocumentCommand(
                () => sender.Send(new BeginDocumentReviewCommand(id, auditContext), ct).AsTask(),
                new DocumentCommandAuditMetadata(auditContext, DocumentAuditOperation.BeginReview, id, null),
                scopeFactory,
                ct),
            id,
            queryService,
            ct);
    }

    [ObserveDocumentOperation(DocumentOperation.RequestChanges)]
    private static async Task<IResult> RequestChanges(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        [FromServices] IDocumentQueryService queryService,
        [FromServices] IServiceScopeFactory scopeFactory,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var auditContext = CreateAuditContext(httpContext);
        return await GetDocumentAfterCommand(
            await ExecuteDocumentCommand(
                () => sender.Send(new RequestDocumentChangesCommand(id, auditContext), ct).AsTask(),
                new DocumentCommandAuditMetadata(auditContext, DocumentAuditOperation.RequestChanges, id, null),
                scopeFactory,
                ct),
            id,
            queryService,
            ct);
    }

    [ObserveDocumentOperation(DocumentOperation.AcknowledgeChanges)]
    private static async Task<IResult> AcknowledgeChanges(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        [FromServices] IDocumentQueryService queryService,
        [FromServices] IServiceScopeFactory scopeFactory,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var auditContext = CreateAuditContext(httpContext);
        return await GetDocumentAfterCommand(
            await ExecuteDocumentCommand(
                () => sender.Send(new AcknowledgeDocumentChangesCommand(id, auditContext), ct).AsTask(),
                new DocumentCommandAuditMetadata(auditContext, DocumentAuditOperation.AcknowledgeChanges, id, null),
                scopeFactory,
                ct),
            id,
            queryService,
            ct);
    }

    [ObserveDocumentOperation(DocumentOperation.UpdateField)]
    private static async Task<IResult> UpdateField(
        [FromRoute] Guid id,
        [FromRoute] string fieldId,
        [FromBody] UpdateDocumentFieldDto dto,
        [FromServices] ISender sender,
        [FromServices] IDocumentQueryService queryService,
        [FromServices] IServiceScopeFactory scopeFactory,
        HttpContext httpContext,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var auditContext = CreateAuditContext(httpContext);

        return await GetDocumentAfterCommand(
            await ExecuteDocumentCommand(
                () => sender.Send(new UpdateDocumentFieldCommand(id, fieldId, dto.Value, auditContext), ct).AsTask(),
                new DocumentCommandAuditMetadata(auditContext, DocumentAuditOperation.UpdateField, id, null),
                scopeFactory,
                ct),
            id,
            queryService,
            ct);
    }

    [ObserveDocumentOperation(DocumentOperation.Approve)]
    private static async Task<IResult> Approve(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        [FromServices] IDocumentQueryService queryService,
        [FromServices] IServiceScopeFactory scopeFactory,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var auditContext = CreateAuditContext(httpContext);
        return await GetDocumentAfterCommand(
            await ExecuteDocumentCommand(
                () => sender.Send(new ApproveDocumentCommand(id, auditContext), ct).AsTask(),
                new DocumentCommandAuditMetadata(auditContext, DocumentAuditOperation.Approve, id, null),
                scopeFactory,
                ct),
            id,
            queryService,
            ct);
    }

    [ObserveDocumentOperation(DocumentOperation.Finalize)]
    private static async Task<IResult> FinalizeDocument(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        [FromServices] IDocumentQueryService queryService,
        [FromServices] IServiceScopeFactory scopeFactory,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var auditContext = CreateAuditContext(httpContext);
        return await GetDocumentAfterCommand(
            await ExecuteDocumentCommand(
                () => sender.Send(new FinalizeDocumentCommand(id, auditContext), ct).AsTask(),
                new DocumentCommandAuditMetadata(auditContext, DocumentAuditOperation.Finalize, id, null),
                scopeFactory,
                ct),
            id,
            queryService,
            ct);
    }

    [ObserveDocumentOperation(DocumentOperation.Regenerate)]
    private static async Task<IResult> Regenerate(
        [FromRoute] Guid id,
        [Required, FromHeader(Name = IdempotencyKeyHeader.Name)] string? idempotencyKeyHeader,
        [FromServices] ISender sender,
        [FromServices] IDocumentQueryService queryService,
        [FromServices] IServiceScopeFactory scopeFactory,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var keyResult = IdempotencyKeyHeader.Create(idempotencyKeyHeader);
        if (keyResult.IsFailure)
        {
            return ToError(keyResult.ConvertError());
        }

        var auditContext = CreateAuditContext(httpContext);
        var result = await ExecuteDocumentCommand(() => sender.Send(
            new RegenerateDocumentDraftCommand(id, auditContext, keyResult.Value),
            ct).AsTask(),
            new DocumentCommandAuditMetadata(auditContext, DocumentAuditOperation.Regenerate, id, null),
            scopeFactory,
            ct);
        if (result.IsFailure)
        {
            return ToError(result.ConvertError());
        }

        var document = await queryService.GetById(result.Value, ct);
        return document is null ? ToError(DocumentErrors.DocumentNotFound()) : TypedResults.Ok(document);
    }

    [ObserveDocumentOperation(DocumentOperation.Void)]
    private static async Task<IResult> Void(
        [FromRoute] Guid id,
        [FromServices] ISender sender,
        [FromServices] IDocumentQueryService queryService,
        [FromServices] IServiceScopeFactory scopeFactory,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var auditContext = CreateAuditContext(httpContext);
        return await GetDocumentAfterCommand(
            await ExecuteDocumentCommand(
                () => sender.Send(new VoidDocumentCommand(id, DocumentVoidReason.AdministrativeVoid, auditContext), ct).AsTask(),
                new DocumentCommandAuditMetadata(auditContext, DocumentAuditOperation.Void, id, null),
                scopeFactory,
                ct),
            id,
            queryService,
            ct);
    }

    [ObserveDocumentOperation(DocumentOperation.Download)]
    private static async Task<IResult> DownloadFinalizedArtifact(
        [FromRoute] Guid id,
        [FromServices] GetFinalizedDocumentArtifactHandler handler,
        [FromServices] IDocumentQueryService queryService,
        [FromServices] DocumentAuditWriter documentAuditWriter,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var auditContext = CreateAuditContext(httpContext);
        var result = await handler.Handle(new GetFinalizedDocumentArtifactQuery(id, auditContext.ActorId), ct);
        if (result.IsFailure)
        {
            var failure = result.ConvertError();
            var metadata = await queryService.GetAuditMetadataById(id, ct);
            var auditResult = await documentAuditWriter.Add(
                auditContext,
                DocumentAuditOperation.Download,
                id,
                metadata?.BookingId,
                metadata?.Revision,
                DocumentAuditOutcome.Rejected,
                GetFailureReasonCode(failure),
                ct);
            return auditResult.IsFailure ? ToError(auditResult) : ToError(failure);
        }

        var artifact = result.Value;
        var successfulAudit = await documentAuditWriter.Add(
            auditContext,
            DocumentAuditOperation.Download,
            artifact.DocumentId,
            artifact.BookingId,
            artifact.Revision,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.None,
            ct);
        if (successfulAudit.IsFailure)
        {
            return ToError(successfulAudit);
        }

        httpContext.Response.Headers.CacheControl = "no-store";
        httpContext.Response.Headers.Pragma = "no-cache";
        httpContext.Response.Headers.Expires = "0";
        httpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
        httpContext.Response.Headers.ContentSecurityPolicy = DocumentArtifactDelivery.ContentSecurityPolicy;
        return TypedResults.Bytes(artifact.Content, DocumentArtifactResponse.ContentType, artifact.FileName, enableRangeProcessing: false);
    }

    private static async Task<IResult> GetDocumentAfterCommand(
        Result result,
        Guid documentId,
        IDocumentQueryService queryService,
        CancellationToken ct)
    {
        if (result.IsFailure)
        {
            return ToError(result);
        }

        var document = await queryService.GetById(documentId, ct);
        return document is null ? ToError(DocumentErrors.DocumentNotFound()) : TypedResults.Ok(document);
    }

    private sealed record DocumentCommandAuditMetadata(
        DocumentAuditContext Context,
        DocumentAuditOperation Operation,
        Guid? DocumentId,
        Guid? BookingId);

    private static async Task<Result> ExecuteDocumentCommand(
        Func<Task<Result>> operation,
        DocumentCommandAuditMetadata auditMetadata,
        IServiceScopeFactory scopeFactory,
        CancellationToken ct)
    {
        try
        {
            return await operation();
        }
        catch (DbUpdateConcurrencyException)
        {
            var auditResult = await RecordRejectedConcurrencyAudit(scopeFactory, auditMetadata, ct);
            return auditResult.IsFailure ? auditResult : DocumentErrors.DocumentChangedByAnotherRequest();
        }
        catch (DocumentRevisionConflictException)
        {
            var auditResult = await RecordRejectedConcurrencyAudit(scopeFactory, auditMetadata, ct);
            return auditResult.IsFailure ? auditResult : DocumentErrors.DocumentRevisionAlreadyExists();
        }
        catch (DocumentBookingEligibilityConflictException)
        {
            var auditResult = await RecordRejectedConcurrencyAudit(scopeFactory, auditMetadata, ct);
            return auditResult.IsFailure ? auditResult : DocumentErrors.BookingIsNotAccepted();
        }
    }

    private static async Task<Result<T>> ExecuteDocumentCommand<T>(
        Func<Task<Result<T>>> operation,
        DocumentCommandAuditMetadata auditMetadata,
        IServiceScopeFactory scopeFactory,
        CancellationToken ct)
        where T : notnull
    {
        try
        {
            return await operation();
        }
        catch (DbUpdateConcurrencyException)
        {
            var auditResult = await RecordRejectedConcurrencyAudit(scopeFactory, auditMetadata, ct);
            return auditResult.IsFailure
                ? auditResult.ConvertError<T>()
                : DocumentErrors.DocumentChangedByAnotherRequest().ConvertError<T>();
        }
        catch (DocumentRevisionConflictException)
        {
            var auditResult = await RecordRejectedConcurrencyAudit(scopeFactory, auditMetadata, ct);
            return auditResult.IsFailure
                ? auditResult.ConvertError<T>()
                : DocumentErrors.DocumentRevisionAlreadyExists().ConvertError<T>();
        }
        catch (DocumentBookingEligibilityConflictException)
        {
            var auditResult = await RecordRejectedConcurrencyAudit(scopeFactory, auditMetadata, ct);
            return auditResult.IsFailure
                ? auditResult.ConvertError<T>()
                : DocumentErrors.BookingIsNotAccepted().ConvertError<T>();
        }
    }

    private static async Task<Result> RecordRejectedConcurrencyAudit(
        IServiceScopeFactory scopeFactory,
        DocumentCommandAuditMetadata metadata,
        CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var documentAuditWriter = serviceProvider.GetRequiredService<DocumentAuditWriter>();
        var bookingId = metadata.BookingId;
        int? revision = null;

        if (metadata.DocumentId is { } documentId)
        {
            var queryService = serviceProvider.GetRequiredService<IDocumentQueryService>();
            var document = await queryService.GetAuditMetadataById(documentId, ct);
            if (document is not null)
            {
                bookingId = document.BookingId;
                revision = document.Revision;
            }
        }

        return await documentAuditWriter.Add(
            metadata.Context,
            metadata.Operation,
            metadata.DocumentId,
            bookingId,
            revision,
            DocumentAuditOutcome.Rejected,
            DocumentAuditReasonCode.StateConflict,
            ct);
    }

    /// <summary>Creates the trusted audit context with the request's server-generated correlation identifier.</summary>
    internal static DocumentAuditContext CreateAuditContext(HttpContext httpContext)
    {
        var actorId = httpContext.User.GetSubjectId();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            throw new InvalidOperationException("Document operations require an authenticated subject identifier.");
        }

        var auditContextResult = DocumentAuditContext.Create(actorId, DocumentEndpointTelemetry.GetCorrelationId(httpContext).ToString("N"));
        if (auditContextResult.IsFailure)
        {
            throw new InvalidOperationException("Document operations require valid audit metadata.");
        }

        return auditContextResult.Value;
    }

    private static DocumentAuditReasonCode GetFailureReasonCode(Result result) => result.Status switch
    {
        ResultStatus.NotFound => DocumentAuditReasonCode.DocumentNotFound,
        ResultStatus.Conflict => DocumentAuditReasonCode.ArtifactUnavailable,
        ResultStatus.Invalid => DocumentAuditReasonCode.ValidationRejected,
        ResultStatus.Unavailable => DocumentAuditReasonCode.BrandingUnavailable,
        _ => DocumentAuditReasonCode.None
    };

    private static IResult ToError(Result result) => result.Status switch
    {
        ResultStatus.NotFound => result.ToNotFound(),
        ResultStatus.Conflict => result.ToConflict(),
        ResultStatus.Invalid => result.ToValidationProblem(),
        ResultStatus.Unavailable => TypedResults.Problem(result.ErrorDetails?.Detail, statusCode: StatusCodes.Status503ServiceUnavailable),
        _ => TypedResults.Problem("The document operation could not be completed.", statusCode: StatusCodes.Status500InternalServerError)
    };
}
