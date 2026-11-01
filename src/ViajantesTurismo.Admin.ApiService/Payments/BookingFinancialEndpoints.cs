using PandaBear.AspNetCore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiService.Payments;

/// <summary>Endpoints for booking financial history, payment schedules and quotes.</summary>
internal static class BookingFinancialEndpoints
{
    /// <summary>Maps the booking financial endpoints under the payment workflow policy.</summary>
    public static void MapBookingFinancialEndpoints(this WebApplication app)
    {
        var group = app.MapBookingsGroup().MapGroup("/{bookingId:guid}/finances").WithPaymentWorkflowPolicy();
        group.MapGet("/", Read).RequireAuthorization(AdminAuthorization.PaymentRead)
            .WithAdminMetadata("GetBookingFinancialHistory", "Retrieves masked financial history and approved EUR balances.", "Retrieves masked booking finances.");
        group.MapGet("/details", ReadSensitive).RequireAuthorization(AdminAuthorization.PaymentManage)
            .WithAdminMetadata("GetBookingFinancialDetails", "Audits Admin access to full settlement evidence references.", "Retrieves sensitive financial evidence.");
        group.MapPost("/schedules", DraftBookingPaymentSchedule).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("DraftBookingPaymentSchedule", "Executes DraftBookingPaymentSchedule with a required idempotency key and revision precondition.", "DraftBookingPaymentSchedule operation.");
        group.MapPost("/schedules/{scheduleId:guid}/approve", ApproveBookingPaymentSchedule).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("ApproveBookingPaymentSchedule", "Executes ApproveBookingPaymentSchedule with a required idempotency key and revision precondition.", "ApproveBookingPaymentSchedule operation.");
        group.MapPost("/quotes", DraftBookingPaymentQuote).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("DraftBookingPaymentQuote", "Executes DraftBookingPaymentQuote with a required idempotency key and revision precondition.", "DraftBookingPaymentQuote operation.");
        group.MapPost("/quotes/{quoteId:guid}/approve", ApproveBookingPaymentQuote).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("ApproveBookingPaymentQuote", "Executes ApproveBookingPaymentQuote with a required idempotency key and revision precondition.", "ApproveBookingPaymentQuote operation.");
        group.MapPost("/receipts", RecordBookingReceipt).RequireAuthorization(AdminAuthorization.PaymentWrite)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("RecordBookingReceipt", "Executes RecordBookingReceipt with a required idempotency key and revision precondition.", "RecordBookingReceipt operation.");
        group.MapPost("/receipts/{receiptId:guid}/reconciliations", DraftBookingReceiptReconciliation).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("DraftBookingReceiptReconciliation", "Executes DraftBookingReceiptReconciliation with a required idempotency key and revision precondition.", "DraftBookingReceiptReconciliation operation.");
        group.MapPost("/reconciliations/{reconciliationId:guid}/approve", ApproveBookingReceiptReconciliation).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("ApproveBookingReceiptReconciliation", "Executes ApproveBookingReceiptReconciliation with a required idempotency key and revision precondition.", "ApproveBookingReceiptReconciliation operation.");
        group.MapPost("/receipts/{receiptId:guid}/allocations", AllocateBookingPayment).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("AllocateBookingPayment", "Executes AllocateBookingPayment with a required idempotency key and revision precondition.", "AllocateBookingPayment operation.");
        group.MapPost("/allocation-releases", ReleaseBookingPaymentAllocations).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("ReleaseBookingPaymentAllocations", "Executes ReleaseBookingPaymentAllocations with a required idempotency key and revision precondition.", "ReleaseBookingPaymentAllocations operation.");
        group.MapPost("/receipts/{receiptId:guid}/reversals", ReverseBookingReceipt).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("ReverseBookingReceipt", "Executes ReverseBookingReceipt with a required idempotency key and revision precondition.", "ReverseBookingReceipt operation.");
        group.MapPost("/receipts/{receiptId:guid}/refunds", DraftBookingPaymentRefund).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("DraftBookingPaymentRefund", "Executes DraftBookingPaymentRefund with a required idempotency key and revision precondition.", "DraftBookingPaymentRefund operation.");
        group.MapPost("/refunds/{refundId:guid}/approve", ApproveBookingPaymentRefund).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("ApproveBookingPaymentRefund", "Executes ApproveBookingPaymentRefund with a required idempotency key and revision precondition.", "ApproveBookingPaymentRefund operation.");
        group.MapPost("/refunds/{refundId:guid}/corrections", CorrectBookingPaymentRefund).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("CorrectBookingPaymentRefund", "Executes CorrectBookingPaymentRefund with a required idempotency key and revision precondition.", "CorrectBookingPaymentRefund operation.");
        group.MapPost("/records/{targetId:guid}/review", ReviewBookingFinancialRecord).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("ReviewBookingFinancialRecord", "Executes ReviewBookingFinancialRecord with a required idempotency key and revision precondition.", "ReviewBookingFinancialRecord operation.");
        group.MapPost("/records/{targetId:guid}/withdraw-approval", WithdrawBookingFinancialApproval).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("WithdrawBookingFinancialApproval", "Executes WithdrawBookingFinancialApproval with a required idempotency key and revision precondition.", "WithdrawBookingFinancialApproval operation.");
    }

    private static async Task<Results<Ok<GetBookingFinancialHistoryDto>, NotFound>> Read(Guid bookingId, [FromServices] IPaymentFinancialQueryService queries, CancellationToken ct)
    {
        var history = await queries.GetByBookingId(bookingId, ct);
        return history is null ? TypedResults.NotFound() : TypedResults.Ok(history);
    }

    private static async Task<Results<Ok<GetBookingFinancialHistoryDto>, NotFound, UnauthorizedHttpResult>> ReadSensitive(Guid bookingId, [FromServices] IPaymentFinancialQueryService queries, HttpContext context, CancellationToken ct)
    {
        var actorId = context.User.GetSubjectId();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return TypedResults.Unauthorized();
        }
        var history = await queries.GetSensitiveDetails(bookingId, actorId, ct);
        return history is null ? TypedResults.NotFound() : TypedResults.Ok(history);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> DraftBookingPaymentSchedule(Guid bookingId, [FromBody] CreatePaymentScheduleDto input, [FromServices] DraftBookingPaymentScheduleCommandHandler handler, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new DraftBookingPaymentScheduleCommand(bookingId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> ApproveBookingPaymentSchedule(Guid bookingId, Guid scheduleId, [FromBody] ApproveFinancialRecordDto input, [FromServices] ApproveBookingPaymentScheduleCommandHandler handler, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new ApproveBookingPaymentScheduleCommand(bookingId, scheduleId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> DraftBookingPaymentQuote(Guid bookingId, [FromBody] CreatePaymentQuoteDto input, [FromServices] DraftBookingPaymentQuoteCommandHandler handler, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new DraftBookingPaymentQuoteCommand(bookingId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> ApproveBookingPaymentQuote(Guid bookingId, Guid quoteId, [FromBody] ApproveFinancialRecordDto input, [FromServices] ApproveBookingPaymentQuoteCommandHandler handler, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new ApproveBookingPaymentQuoteCommand(bookingId, quoteId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> RecordBookingReceipt(Guid bookingId, [FromBody] CreateBookingReceiptDto input, [FromServices] RecordBookingReceiptCommandHandler handler, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new RecordBookingReceiptCommand(bookingId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> DraftBookingReceiptReconciliation(Guid bookingId, Guid receiptId, [FromBody] CreateReceiptReconciliationDto input, [FromServices] DraftBookingReceiptReconciliationCommandHandler handler, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new DraftBookingReceiptReconciliationCommand(bookingId, receiptId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> ApproveBookingReceiptReconciliation(Guid bookingId, Guid reconciliationId, [FromBody] ApproveFinancialRecordDto input, [FromServices] ApproveBookingReceiptReconciliationCommandHandler handler, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new ApproveBookingReceiptReconciliationCommand(bookingId, reconciliationId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> AllocateBookingPayment(Guid bookingId, Guid receiptId, [FromBody] AllocatePaymentCreditDto input, [FromServices] AllocateBookingPaymentCommandHandler handler, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new AllocateBookingPaymentCommand(bookingId, receiptId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> ReleaseBookingPaymentAllocations(Guid bookingId, [FromBody] AllocatePaymentCreditDto input, [FromServices] ReleaseBookingPaymentAllocationsCommandHandler handler, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new ReleaseBookingPaymentAllocationsCommand(bookingId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> ReverseBookingReceipt(Guid bookingId, Guid receiptId, [FromBody] ApproveFinancialRecordDto input, [FromServices] ReverseBookingReceiptCommandHandler handler, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new ReverseBookingReceiptCommand(bookingId, receiptId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> DraftBookingPaymentRefund(Guid bookingId, Guid receiptId, [FromBody] CreatePaymentRefundDto input, [FromServices] DraftBookingPaymentRefundCommandHandler handler, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new DraftBookingPaymentRefundCommand(bookingId, receiptId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> ApproveBookingPaymentRefund(Guid bookingId, Guid refundId, [FromBody] ApproveFinancialRecordDto input, [FromServices] ApproveBookingPaymentRefundCommandHandler handler, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new ApproveBookingPaymentRefundCommand(bookingId, refundId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> CorrectBookingPaymentRefund(Guid bookingId, Guid refundId, [FromBody] ApproveFinancialRecordDto input, [FromServices] CorrectBookingPaymentRefundCommandHandler handler, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new CorrectBookingPaymentRefundCommand(bookingId, refundId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> ReviewBookingFinancialRecord(Guid bookingId, Guid targetId, [FromBody] ReviewFinancialRecordDto input, [FromServices] ReviewBookingFinancialRecordCommandHandler handler, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new ReviewBookingFinancialRecordCommand(bookingId, targetId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> WithdrawBookingFinancialApproval(Guid bookingId, Guid targetId, [FromBody] ApproveFinancialRecordDto input, [FromServices] WithdrawBookingFinancialApprovalCommandHandler handler, HttpContext context, CancellationToken ct)
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
        var result = await handler.Handle(new WithdrawBookingFinancialApprovalCommand(bookingId, targetId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult> Respond(Guid bookingId, Result<Guid> result) => result.IsSuccess
        ? TypedResults.Ok(new GetPaymentCommandResultDto { BookingId = bookingId, ResourceId = result.Value })
        : result.Status switch
        {
            ResultStatus.NotFound => result.ToNotFound(),
            ResultStatus.Conflict => result.ToConflict(),
            ResultStatus.Invalid => result.ToValidationProblem(),
            _ => throw new InvalidOperationException("Unexpected financial command result.")
        };
}
