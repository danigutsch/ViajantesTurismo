using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.ApiService.Documents;

/// <summary>Defines Admin-only endpoints for generated document lifecycle operations.</summary>
internal static class DocumentEndpoints
{
    private const string ContractTemplateId = "tour-service-contract";
    private const string ContractTemplateVersion = "1";
    private const string AdministrativeVoidReasonCode = "administrative-void";
    private const string HtmlMediaType = "text/html; charset=utf-8";

    /// <summary>Maps generated-document endpoints to the Admin API.</summary>
    public static void MapDocumentEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var documentsGroup = app.MapDocumentsGroup();

        documentsGroup.MapPost("/bookings/{bookingId:guid}/contract-drafts", GenerateContractDraft)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.DocumentManage)
            .Produces<GetDocumentDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
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

        documentsGroup.MapPatch("/{id:guid}/fields/{fieldId}", UpdateField)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.DocumentManage)
            .Produces<GetDocumentDto>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
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
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
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
            .Produces(StatusCodes.Status200OK, contentType: HtmlMediaType)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .WithAdminMetadata("DownloadDocumentArtifact", "Downloads a finalized document artifact through a mediated response.", "Downloads a document artifact.");
    }

    private static async Task<IResult> GenerateContractDraft(
        [FromRoute] Guid bookingId,
        [FromServices] GenerateContractDraftCommandHandler handler,
        [FromServices] IDocumentQueryService queryService,
        [FromServices] IServiceScopeFactory scopeFactory,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var auditContext = CreateAuditContext(httpContext);
        var result = await ExecuteDocumentCommand(() => handler.Handle(
            new GenerateContractDraftCommand(
                bookingId,
                ContractTemplateId,
                ContractTemplateVersion,
                auditContext),
            ct),
            new DocumentCommandAuditMetadata(auditContext, DocumentAuditOperation.Generate, null, bookingId),
            scopeFactory,
            ct);
        if (result.IsFailure)
        {
            return ToError(result.ConvertError());
        }

        var document = await queryService.GetById(result.Value, ct);
        return document is null
            ? ToError(DocumentErrors.DocumentNotFound(result.Value))
            : TypedResults.Created($"/api/v1/documents/{document.Id}", document);
    }

    private static async Task<IResult> GetDocumentById(
        [FromRoute] Guid id,
        [FromServices] IDocumentQueryService queryService,
        [FromServices] IDocumentAuditStore auditStore,
        [FromServices] IUnitOfWork unitOfWork,
        [FromServices] TimeProvider timeProvider,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var auditContext = CreateAuditContext(httpContext);
        var document = await queryService.GetById(id, ct);
        if (document is null)
        {
            var auditResult = await RecordEndpointAudit(
                auditStore,
                unitOfWork,
                auditContext,
                DocumentAuditOperation.Read,
                id,
                null,
                null,
                DocumentAuditOutcome.Rejected,
                DocumentAuditReasonCode.DocumentNotFound,
                timeProvider,
                ct);
            return auditResult.IsFailure ? ToError(auditResult) : ToError(DocumentErrors.DocumentNotFound(id));
        }

        var successfulAudit = await RecordEndpointAudit(
            auditStore,
            unitOfWork,
            auditContext,
            DocumentAuditOperation.Read,
            document.Id,
            document.BookingId,
            document.Revision,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.None,
            timeProvider,
            ct);
        return successfulAudit.IsFailure ? ToError(successfulAudit) : TypedResults.Ok(document);
    }

    private static async Task<IResult> BeginReview(
        [FromRoute] Guid id,
        [FromServices] BeginDocumentReviewCommandHandler handler,
        [FromServices] IDocumentQueryService queryService,
        [FromServices] IServiceScopeFactory scopeFactory,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var auditContext = CreateAuditContext(httpContext);
        return await GetDocumentAfterCommand(
            await ExecuteDocumentCommand(
                () => handler.Handle(new BeginDocumentReviewCommand(id, auditContext), ct),
                new DocumentCommandAuditMetadata(auditContext, DocumentAuditOperation.BeginReview, id, null),
                scopeFactory,
                ct),
            id,
            queryService,
            ct);
    }

    private static async Task<IResult> RequestChanges(
        [FromRoute] Guid id,
        [FromServices] RequestDocumentChangesCommandHandler handler,
        [FromServices] IDocumentQueryService queryService,
        [FromServices] IServiceScopeFactory scopeFactory,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var auditContext = CreateAuditContext(httpContext);
        return await GetDocumentAfterCommand(
            await ExecuteDocumentCommand(
                () => handler.Handle(new RequestDocumentChangesCommand(id, auditContext), ct),
                new DocumentCommandAuditMetadata(auditContext, DocumentAuditOperation.RequestChanges, id, null),
                scopeFactory,
                ct),
            id,
            queryService,
            ct);
    }

    private static async Task<IResult> UpdateField(
        [FromRoute] Guid id,
        [FromRoute] string fieldId,
        [FromBody] UpdateDocumentFieldDto dto,
        [FromServices] UpdateDocumentFieldCommandHandler handler,
        [FromServices] IDocumentQueryService queryService,
        [FromServices] IServiceScopeFactory scopeFactory,
        HttpContext httpContext,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var auditContext = CreateAuditContext(httpContext);

        return await GetDocumentAfterCommand(
            await ExecuteDocumentCommand(
                () => handler.Handle(new UpdateDocumentFieldCommand(id, fieldId, dto.Value, auditContext), ct),
                new DocumentCommandAuditMetadata(auditContext, DocumentAuditOperation.UpdateField, id, null),
                scopeFactory,
                ct),
            id,
            queryService,
            ct);
    }

    private static async Task<IResult> Approve(
        [FromRoute] Guid id,
        [FromServices] ApproveDocumentCommandHandler handler,
        [FromServices] IDocumentQueryService queryService,
        [FromServices] IServiceScopeFactory scopeFactory,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var auditContext = CreateAuditContext(httpContext);
        return await GetDocumentAfterCommand(
            await ExecuteDocumentCommand(
                () => handler.Handle(new ApproveDocumentCommand(id, auditContext), ct),
                new DocumentCommandAuditMetadata(auditContext, DocumentAuditOperation.Approve, id, null),
                scopeFactory,
                ct),
            id,
            queryService,
            ct);
    }

    private static async Task<IResult> FinalizeDocument(
        [FromRoute] Guid id,
        [FromServices] FinalizeDocumentCommandHandler handler,
        [FromServices] IDocumentQueryService queryService,
        [FromServices] IServiceScopeFactory scopeFactory,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var auditContext = CreateAuditContext(httpContext);
        return await GetDocumentAfterCommand(
            await ExecuteDocumentCommand(
                () => handler.Handle(new FinalizeDocumentCommand(id, auditContext), ct),
                new DocumentCommandAuditMetadata(auditContext, DocumentAuditOperation.Finalize, id, null),
                scopeFactory,
                ct),
            id,
            queryService,
            ct);
    }

    private static async Task<IResult> Regenerate(
        [FromRoute] Guid id,
        [FromServices] RegenerateDocumentDraftCommandHandler handler,
        [FromServices] IDocumentQueryService queryService,
        [FromServices] IServiceScopeFactory scopeFactory,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var auditContext = CreateAuditContext(httpContext);
        var result = await ExecuteDocumentCommand(() => handler.Handle(
            new RegenerateDocumentDraftCommand(id, ContractTemplateId, ContractTemplateVersion, auditContext),
            ct),
            new DocumentCommandAuditMetadata(auditContext, DocumentAuditOperation.Regenerate, id, null),
            scopeFactory,
            ct);
        if (result.IsFailure)
        {
            return ToError(result.ConvertError());
        }

        var document = await queryService.GetById(result.Value, ct);
        return document is null ? ToError(DocumentErrors.DocumentNotFound(result.Value)) : TypedResults.Ok(document);
    }

    private static async Task<IResult> Void(
        [FromRoute] Guid id,
        [FromServices] VoidDocumentCommandHandler handler,
        [FromServices] IDocumentQueryService queryService,
        [FromServices] IServiceScopeFactory scopeFactory,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var auditContext = CreateAuditContext(httpContext);
        return await GetDocumentAfterCommand(
            await ExecuteDocumentCommand(
                () => handler.Handle(new VoidDocumentCommand(id, AdministrativeVoidReasonCode, auditContext), ct),
                new DocumentCommandAuditMetadata(auditContext, DocumentAuditOperation.Void, id, null),
                scopeFactory,
                ct),
            id,
            queryService,
            ct);
    }

    private static async Task<IResult> DownloadFinalizedArtifact(
        [FromRoute] Guid id,
        [FromServices] GetFinalizedDocumentArtifactHandler handler,
        [FromServices] IDocumentAuditStore auditStore,
        [FromServices] IUnitOfWork unitOfWork,
        [FromServices] TimeProvider timeProvider,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var auditContext = CreateAuditContext(httpContext);
        var result = await handler.Handle(new GetFinalizedDocumentArtifactQuery(id), ct);
        if (result.IsFailure)
        {
            var failure = result.ConvertError();
            var auditResult = await RecordEndpointAudit(
                auditStore,
                unitOfWork,
                auditContext,
                DocumentAuditOperation.Download,
                id,
                null,
                null,
                DocumentAuditOutcome.Rejected,
                GetFailureReasonCode(failure),
                timeProvider,
                ct);
            return auditResult.IsFailure ? ToError(auditResult) : ToError(failure);
        }

        var artifact = result.Value;
        var successfulAudit = await RecordEndpointAudit(
            auditStore,
            unitOfWork,
            auditContext,
            DocumentAuditOperation.Download,
            artifact.DocumentId,
            artifact.BookingId,
            artifact.Revision,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.None,
            timeProvider,
            ct);
        if (successfulAudit.IsFailure)
        {
            return ToError(successfulAudit);
        }

        httpContext.Response.Headers.CacheControl = "no-store";
        httpContext.Response.Headers.Pragma = "no-cache";
        httpContext.Response.Headers.Expires = "0";
        httpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
        return TypedResults.File(artifact.Content.ToArray(), HtmlMediaType, artifact.FileName, enableRangeProcessing: false);
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
        return document is null ? ToError(DocumentErrors.DocumentNotFound(documentId)) : TypedResults.Ok(document);
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
    }

    private static async Task<Result> RecordRejectedConcurrencyAudit(
        IServiceScopeFactory scopeFactory,
        DocumentCommandAuditMetadata metadata,
        CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var auditStore = serviceProvider.GetRequiredService<IDocumentAuditStore>();
        var unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
        var timeProvider = serviceProvider.GetRequiredService<TimeProvider>();
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

        return await RecordEndpointAudit(
            auditStore,
            unitOfWork,
            metadata.Context,
            metadata.Operation,
            metadata.DocumentId,
            bookingId,
            revision,
            DocumentAuditOutcome.Rejected,
            DocumentAuditReasonCode.StateConflict,
            timeProvider,
            ct);
    }

    private static async Task<Result> RecordEndpointAudit(
        IDocumentAuditStore auditStore,
        IUnitOfWork unitOfWork,
        DocumentAuditContext auditContext,
        DocumentAuditOperation operation,
        Guid? documentId,
        Guid? bookingId,
        int? revision,
        DocumentAuditOutcome outcome,
        DocumentAuditReasonCode reasonCode,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        var auditResult = DocumentAuditWriter.Add(
            auditStore,
            auditContext,
            operation,
            documentId,
            bookingId,
            revision,
            outcome,
            reasonCode,
            timeProvider.GetUtcNow().UtcDateTime);
        if (auditResult.IsFailure)
        {
            return auditResult.ConvertError();
        }

        if (auditResult.Value)
        {
            await unitOfWork.SaveEntities(ct);
        }

        return Result.Ok();
    }

    private static DocumentAuditContext CreateAuditContext(HttpContext httpContext)
    {
        var actorId = httpContext.User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(actorId))
        {
            throw new InvalidOperationException("Document operations require an authenticated subject identifier.");
        }

        var correlationId = Guid.CreateVersion7().ToString("N");
        return new DocumentAuditContext(actorId, correlationId);
    }

    private static DocumentAuditReasonCode GetFailureReasonCode(Result result) => result.Status switch
    {
        ResultStatus.NotFound => DocumentAuditReasonCode.DocumentNotFound,
        ResultStatus.Conflict => DocumentAuditReasonCode.ArtifactUnavailable,
        ResultStatus.Invalid => DocumentAuditReasonCode.ValidationRejected,
        _ => DocumentAuditReasonCode.None
    };

    private static IResult ToError(Result result) => result.Status switch
    {
        ResultStatus.NotFound => result.ToNotFound(),
        ResultStatus.Conflict => result.ToConflict(),
        ResultStatus.Invalid => result.ToValidationProblem(),
        _ => TypedResults.Problem("The document operation could not be completed.", statusCode: StatusCodes.Status500InternalServerError)
    };
}
