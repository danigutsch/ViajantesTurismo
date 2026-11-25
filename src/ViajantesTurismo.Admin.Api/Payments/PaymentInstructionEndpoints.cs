using SharedKernel.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Api.Documents;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Infrastructure.Documents;

namespace ViajantesTurismo.Admin.Api.Payments;

/// <summary>Endpoints for generating and reviewing payment instructions.</summary>
internal static class PaymentInstructionEndpoints
{
    /// <summary>Maps the payment instruction endpoints under the payment workflow policy.</summary>
    public static void MapPaymentInstructionEndpoints(this WebApplication app)
    {
        var documentsGroup = app.MapDocumentsGroup().WithPaymentWorkflowPolicy().ObserveDocumentOperations();
        documentsGroup.MapGet("/bookings/{bookingId:guid}/payment-instructions", List).RequireAuthorization(AdminAuthorization.PaymentManage)
            .WithAdminMetadata("ListBookingPaymentInstructions", "Lists retained protected payment instruction revisions.", "Lists booking payment instructions.");
        documentsGroup.MapGet("/bookings/{bookingId:guid}/payment-instructions/page", ListPage).RequireAuthorization(AdminAuthorization.PaymentManage)
            .WithAdminMetadata("ListBookingPaymentInstructionPage", "Lists up to 100 masked retained instruction revisions, newest first.", "Lists a page of booking payment instructions.");
        documentsGroup.MapGet("/{id:guid}/payment-instructions", Read).RequireAuthorization(AdminAuthorization.PaymentManage)
            .WithAdminMetadata("GetPaymentInstructions", "Retrieves masked generated instruction fields and approval metadata.", "Retrieves payment instructions.");
        documentsGroup.MapPost("/bookings/{bookingId:guid}/payment-instructions", Generate).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .WithAdminMetadata("GeneratePaymentInstructions", "Generates a new immutable protected instruction draft.", "Generates payment instructions.");
        documentsGroup.MapPost("/bookings/{bookingId:guid}/payment-instructions/{id:guid}/regenerate", Regenerate).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .WithAdminMetadata("RegeneratePaymentInstructions", "Creates an immutable replacement from current approved sources.", "Regenerates payment instructions.");
        documentsGroup.MapPost("/{id:guid}/payment-instructions/approve", ApprovePaymentInstructions).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("ApprovePaymentInstructions", "Executes a confirmed, revision-preconditioned ApprovePaymentInstructions operation.", "ApprovePaymentInstructions operation.");
        documentsGroup.MapPost("/{id:guid}/payment-instructions/finalize", FinalizePaymentInstructions).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("FinalizePaymentInstructions", "Executes a confirmed, revision-preconditioned FinalizePaymentInstructions operation.", "FinalizePaymentInstructions operation.");
        documentsGroup.MapPost("/{id:guid}/payment-instructions/withdraw-approval", WithdrawPaymentInstructionApproval).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("WithdrawPaymentInstructionApproval", "Executes a confirmed, revision-preconditioned WithdrawPaymentInstructionApproval operation.", "WithdrawPaymentInstructionApproval operation.");
        documentsGroup.MapPost("/{id:guid}/payment-instructions/void", VoidPaymentInstructions).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("VoidPaymentInstructions", "Executes a confirmed, revision-preconditioned VoidPaymentInstructions operation.", "VoidPaymentInstructions operation.");
        documentsGroup.MapPost("/{id:guid}/payment-instructions/review", ReviewPaymentInstructions).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("ReviewPaymentInstructions", "Executes a confirmed, revision-preconditioned ReviewPaymentInstructions operation.", "ReviewPaymentInstructions operation.");
    }

    private static async Task<Ok<IReadOnlyList<GetPaymentInstructionDto>>> List(Guid bookingId, [FromServices] PaymentInstructionWorkflow workflow, CancellationToken ct) => TypedResults.Ok(await workflow.ListForBooking(bookingId, ct));

    private static async Task<Results<Ok<GetPaymentInstructionPageDto>, ValidationProblem>> ListPage(Guid bookingId,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromServices] PaymentInstructionWorkflow workflow, CancellationToken ct)
    {
        var size = pageSize ?? ContractConstants.DefaultPaymentInstructionPageSize;
        if (size is < 1 or > ContractConstants.MaxPaymentInstructionPageSize)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["pageSize"] = [$"Page size must be between 1 and {ContractConstants.MaxPaymentInstructionPageSize}."]
            });
        }

        return TypedResults.Ok(await workflow.ListPageForBooking(bookingId, page, size, ct));
    }

    [ObserveDocumentOperation(DocumentOperation.Preview)]
    private static async Task<Results<Ok<GetPaymentInstructionDto>, NotFound>> Read(Guid id, [FromServices] PaymentInstructionWorkflow workflow, CancellationToken ct)
    {
        var instruction = await workflow.GetById(id, ct);
        return instruction is null ? TypedResults.NotFound() : TypedResults.Ok(instruction);
    }

    [ObserveDocumentOperation(DocumentOperation.Generate)]
    private static async Task<Results<Created<GetPaymentInstructionDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, ProblemHttpResult, UnauthorizedHttpResult>> Generate(Guid bookingId, [FromBody] GeneratePaymentInstructionsDto input, [FromServices] ISender sender, [FromServices] PaymentInstructionWorkflow workflow, HttpContext context, CancellationToken ct)
    {
        var actorId = context.User.GetSubjectId();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return TypedResults.Unauthorized();
        }
        var key = PaymentEndpointPolicy.ReadIdempotencyKey(context);
        if (key.IsFailure)
        {
            return key.ToValidationProblem();
        }
        var result = await sender.Send(new GeneratePaymentInstructionsCommand(bookingId, null, input, actorId, key.Value), ct);
        return await RespondCreated(result, workflow, ct);
    }

    [ObserveDocumentOperation(DocumentOperation.Regenerate)]
    private static async Task<Results<Created<GetPaymentInstructionDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, ProblemHttpResult, UnauthorizedHttpResult>> Regenerate(Guid bookingId, Guid id, [FromBody] GeneratePaymentInstructionsDto input, [FromServices] ISender sender, [FromServices] PaymentInstructionWorkflow workflow, HttpContext context, CancellationToken ct)
    {
        var actorId = context.User.GetSubjectId();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return TypedResults.Unauthorized();
        }
        var key = PaymentEndpointPolicy.ReadIdempotencyKey(context);
        if (key.IsFailure)
        {
            return key.ToValidationProblem();
        }
        var result = await sender.Send(new GeneratePaymentInstructionsCommand(bookingId, id, input, actorId, key.Value), ct);
        return await RespondCreated(result, workflow, ct);
    }

    [ObserveDocumentOperation(DocumentOperation.Approve)]
    private static async Task<Results<Ok<GetPaymentInstructionDto>, Created<GetPaymentInstructionDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> ApprovePaymentInstructions(Guid id, [FromBody] UpdatePaymentInstructionsDto input, [FromServices] PaymentInstructionWorkflow workflow, HttpContext context, CancellationToken ct)
    {
        var actorId = context.User.GetSubjectId();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return TypedResults.Unauthorized();
        }
        var key = PaymentEndpointPolicy.ReadIdempotencyKey(context);
        if (key.IsFailure)
        {
            return key.ToValidationProblem();
        }
        var result = await workflow.Change(id, input, actorId, key.Value, PaymentInstructionAuditOperation.Approved, ct);
        return await Respond(result, workflow, ct);
    }

    [ObserveDocumentOperation(DocumentOperation.Finalize)]
    private static async Task<Results<Ok<GetPaymentInstructionDto>, Created<GetPaymentInstructionDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> FinalizePaymentInstructions(Guid id, [FromBody] UpdatePaymentInstructionsDto input, [FromServices] PaymentInstructionWorkflow workflow, HttpContext context, CancellationToken ct)
    {
        var actorId = context.User.GetSubjectId();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return TypedResults.Unauthorized();
        }
        var key = PaymentEndpointPolicy.ReadIdempotencyKey(context);
        if (key.IsFailure)
        {
            return key.ToValidationProblem();
        }
        var result = await workflow.Change(id, input, actorId, key.Value, PaymentInstructionAuditOperation.Finalized, ct);
        return await Respond(result, workflow, ct);
    }

    [ObserveDocumentOperation(DocumentOperation.WithdrawApproval)]
    private static async Task<Results<Ok<GetPaymentInstructionDto>, Created<GetPaymentInstructionDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> WithdrawPaymentInstructionApproval(Guid id, [FromBody] UpdatePaymentInstructionsDto input, [FromServices] PaymentInstructionWorkflow workflow, HttpContext context, CancellationToken ct)
    {
        var actorId = context.User.GetSubjectId();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return TypedResults.Unauthorized();
        }
        var key = PaymentEndpointPolicy.ReadIdempotencyKey(context);
        if (key.IsFailure)
        {
            return key.ToValidationProblem();
        }
        var result = await workflow.Change(id, input, actorId, key.Value, PaymentInstructionAuditOperation.ApprovalWithdrawn, ct);
        return await Respond(result, workflow, ct);
    }

    [ObserveDocumentOperation(DocumentOperation.Void)]
    private static async Task<Results<Ok<GetPaymentInstructionDto>, Created<GetPaymentInstructionDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> VoidPaymentInstructions(Guid id, [FromBody] UpdatePaymentInstructionsDto input, [FromServices] PaymentInstructionWorkflow workflow, HttpContext context, CancellationToken ct)
    {
        var actorId = context.User.GetSubjectId();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return TypedResults.Unauthorized();
        }
        var key = PaymentEndpointPolicy.ReadIdempotencyKey(context);
        if (key.IsFailure)
        {
            return key.ToValidationProblem();
        }
        var result = await workflow.Change(id, input, actorId, key.Value, PaymentInstructionAuditOperation.Voided, ct);
        return await Respond(result, workflow, ct);
    }

    [ObserveDocumentOperation(DocumentOperation.Review)]
    private static async Task<Results<Ok<GetPaymentInstructionDto>, Created<GetPaymentInstructionDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> ReviewPaymentInstructions(Guid id, [FromBody] ReviewPaymentInstructionsDto input, [FromServices] PaymentInstructionWorkflow workflow, HttpContext context, CancellationToken ct)
    {
        var actorId = context.User.GetSubjectId();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return TypedResults.Unauthorized();
        }
        var key = PaymentEndpointPolicy.ReadIdempotencyKey(context);
        if (key.IsFailure)
        {
            return key.ToValidationProblem();
        }
        var result = await workflow.Review(id, input, actorId, key.Value, ct);
        return await Respond(result, workflow, ct);
    }

    private static async Task<Results<Ok<GetPaymentInstructionDto>, Created<GetPaymentInstructionDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> Respond(Result<Guid> result, PaymentInstructionWorkflow workflow, CancellationToken ct)
    {
        if (result.IsFailure)
        {
            return result.Status switch
            {
                ResultStatus.NotFound => result.ToNotFound(),
                ResultStatus.Conflict => result.ToConflict(),
                ResultStatus.Invalid => result.ToValidationProblem(),
                _ => throw new InvalidOperationException("Unexpected payment instruction result.")
            };
        }
        var instruction = await workflow.GetById(result.Value, ct);
        if (instruction is null)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Payment instruction not found.", Status = StatusCodes.Status404NotFound });
        }
        return TypedResults.Ok(instruction);
    }

    private static async Task<Results<Created<GetPaymentInstructionDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, ProblemHttpResult, UnauthorizedHttpResult>> RespondCreated(Result<Guid> result, PaymentInstructionWorkflow workflow, CancellationToken ct)
    {
        if (result.IsFailure)
        {
            return result.Status switch
            {
                ResultStatus.NotFound => result.ToNotFound(),
                ResultStatus.Conflict => result.ToConflict(),
                ResultStatus.Invalid => result.ToValidationProblem(),
                ResultStatus.Unavailable => TypedResults.Problem(result.ErrorDetails?.Detail, statusCode: StatusCodes.Status503ServiceUnavailable),
                _ => throw new InvalidOperationException("Unexpected payment instruction result.")
            };
        }
        var instruction = await workflow.GetById(result.Value, ct);
        if (instruction is null)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Payment instruction not found.", Status = StatusCodes.Status404NotFound });
        }
        return TypedResults.Created("/api/v1/documents/" + result.Value + "/payment-instructions", instruction);
    }
}
