using PandaBear.AspNetCore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiService.Payments;

/// <summary>Endpoints for generating and reviewing payment instructions.</summary>
internal static class PaymentInstructionEndpoints
{
    /// <summary>Maps the payment instruction endpoints under the payment workflow policy.</summary>
    public static void MapPaymentInstructionEndpoints(this WebApplication app)
    {
        var group = app.MapDocumentsGroup().WithPaymentWorkflowPolicy();
        group.MapGet("/bookings/{bookingId:guid}/payment-instructions", List).RequireAuthorization(AdminAuthorization.PaymentManage)
            .WithAdminMetadata("ListBookingPaymentInstructions", "Lists retained protected payment instruction revisions.", "Lists booking payment instructions.");
        group.MapGet("/{id:guid}/payment-instructions", Read).RequireAuthorization(AdminAuthorization.PaymentManage)
            .WithAdminMetadata("GetPaymentInstructions", "Retrieves masked generated instruction fields and approval metadata.", "Retrieves payment instructions.");
        group.MapPost("/bookings/{bookingId:guid}/payment-instructions", Generate).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .WithAdminMetadata("GeneratePaymentInstructions", "Generates a new immutable protected instruction draft.", "Generates payment instructions.");
        group.MapPost("/bookings/{bookingId:guid}/payment-instructions/{id:guid}/regenerate", Regenerate).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .WithAdminMetadata("RegeneratePaymentInstructions", "Creates an immutable replacement from current approved sources.", "Regenerates payment instructions.");
        group.MapPost("/{id:guid}/payment-instructions/approve", ApprovePaymentInstructions).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("ApprovePaymentInstructions", "Executes a confirmed, revision-preconditioned ApprovePaymentInstructions operation.", "ApprovePaymentInstructions operation.");
        group.MapPost("/{id:guid}/payment-instructions/finalize", FinalizePaymentInstructions).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("FinalizePaymentInstructions", "Executes a confirmed, revision-preconditioned FinalizePaymentInstructions operation.", "FinalizePaymentInstructions operation.");
        group.MapPost("/{id:guid}/payment-instructions/withdraw-approval", WithdrawPaymentInstructionApproval).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("WithdrawPaymentInstructionApproval", "Executes a confirmed, revision-preconditioned WithdrawPaymentInstructionApproval operation.", "WithdrawPaymentInstructionApproval operation.");
        group.MapPost("/{id:guid}/payment-instructions/void", VoidPaymentInstructions).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("VoidPaymentInstructions", "Executes a confirmed, revision-preconditioned VoidPaymentInstructions operation.", "VoidPaymentInstructions operation.");
        group.MapPost("/{id:guid}/payment-instructions/review", ReviewPaymentInstructions).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("ReviewPaymentInstructions", "Executes a confirmed, revision-preconditioned ReviewPaymentInstructions operation.", "ReviewPaymentInstructions operation.");
    }

    private static async Task<Ok<IReadOnlyList<GetPaymentInstructionDto>>> List(Guid bookingId, [FromServices] PaymentInstructionWorkflow workflow, CancellationToken ct) => TypedResults.Ok(await workflow.ListForBooking(bookingId, ct));

    private static async Task<Results<Ok<GetPaymentInstructionDto>, NotFound>> Read(Guid id, [FromServices] PaymentInstructionWorkflow workflow, CancellationToken ct)
    {
        var instruction = await workflow.GetById(id, ct);
        return instruction is null ? TypedResults.NotFound() : TypedResults.Ok(instruction);
    }

    private static async Task<Results<Created<GetPaymentInstructionDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, ProblemHttpResult, UnauthorizedHttpResult>> Generate(Guid bookingId, [FromBody] GeneratePaymentInstructionsDto input, [FromServices] GeneratePaymentInstructionsCommandHandler handler, [FromServices] PaymentInstructionWorkflow workflow, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new GeneratePaymentInstructionsCommand(bookingId, null, input, actorId, key.Value), ct);
        return await RespondCreated(result, workflow, ct);
    }

    private static async Task<Results<Created<GetPaymentInstructionDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, ProblemHttpResult, UnauthorizedHttpResult>> Regenerate(Guid bookingId, Guid id, [FromBody] GeneratePaymentInstructionsDto input, [FromServices] GeneratePaymentInstructionsCommandHandler handler, [FromServices] PaymentInstructionWorkflow workflow, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new GeneratePaymentInstructionsCommand(bookingId, id, input, actorId, key.Value), ct);
        return await RespondCreated(result, workflow, ct);
    }

    private static async Task<Results<Ok<GetPaymentInstructionDto>, Created<GetPaymentInstructionDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> ApprovePaymentInstructions(Guid id, [FromBody] UpdatePaymentInstructionsDto input, [FromServices] ApprovePaymentInstructionsCommandHandler handler, [FromServices] PaymentInstructionWorkflow workflow, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new ApprovePaymentInstructionsCommand(id, input, actorId, key.Value), ct);
        return await Respond(result, workflow, ct);
    }

    private static async Task<Results<Ok<GetPaymentInstructionDto>, Created<GetPaymentInstructionDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> FinalizePaymentInstructions(Guid id, [FromBody] UpdatePaymentInstructionsDto input, [FromServices] FinalizePaymentInstructionsCommandHandler handler, [FromServices] PaymentInstructionWorkflow workflow, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new FinalizePaymentInstructionsCommand(id, input, actorId, key.Value), ct);
        return await Respond(result, workflow, ct);
    }

    private static async Task<Results<Ok<GetPaymentInstructionDto>, Created<GetPaymentInstructionDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> WithdrawPaymentInstructionApproval(Guid id, [FromBody] UpdatePaymentInstructionsDto input, [FromServices] WithdrawPaymentInstructionApprovalCommandHandler handler, [FromServices] PaymentInstructionWorkflow workflow, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new WithdrawPaymentInstructionApprovalCommand(id, input, actorId, key.Value), ct);
        return await Respond(result, workflow, ct);
    }

    private static async Task<Results<Ok<GetPaymentInstructionDto>, Created<GetPaymentInstructionDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> VoidPaymentInstructions(Guid id, [FromBody] UpdatePaymentInstructionsDto input, [FromServices] VoidPaymentInstructionsCommandHandler handler, [FromServices] PaymentInstructionWorkflow workflow, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new VoidPaymentInstructionsCommand(id, input, actorId, key.Value), ct);
        return await Respond(result, workflow, ct);
    }

    private static async Task<Results<Ok<GetPaymentInstructionDto>, Created<GetPaymentInstructionDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> ReviewPaymentInstructions(Guid id, [FromBody] ReviewPaymentInstructionsDto input, [FromServices] ReviewPaymentInstructionsCommandHandler handler, [FromServices] PaymentInstructionWorkflow workflow, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new ReviewPaymentInstructionsCommand(id, input, actorId, key.Value), ct);
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
