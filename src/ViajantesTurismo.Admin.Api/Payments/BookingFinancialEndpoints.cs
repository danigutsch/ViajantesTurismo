using SharedKernel.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Api.Payments;

/// <summary>Endpoints for booking financial history, payment schedules and quotes.</summary>
internal static class BookingFinancialEndpoints
{
    /// <summary>Maps the booking financial endpoints under the payment workflow policy.</summary>
    public static void MapBookingFinancialEndpoints(this WebApplication app)
    {
        var bookingFinancesGroup = app.MapBookingsGroup().MapGroup("/{bookingId:guid}/finances").WithPaymentWorkflowPolicy();
        bookingFinancesGroup.MapGet("/", Read).RequireAuthorization(AdminAuthorization.PaymentRead)
            .WithAdminMetadata("GetBookingFinancialHistory", "Retrieves masked financial history and approved EUR balances.", "Retrieves masked booking finances.");
        bookingFinancesGroup.MapGet("/details", ReadSensitive).RequireAuthorization(AdminAuthorization.PaymentManage)
            .WithAdminMetadata("GetBookingFinancialDetails", "Audits Admin access to full settlement evidence references.", "Retrieves sensitive financial evidence.");
        bookingFinancesGroup.MapPost("/schedules", DraftBookingPaymentSchedule).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("DraftBookingPaymentSchedule", "Executes DraftBookingPaymentSchedule with a required idempotency key and revision precondition.", "DraftBookingPaymentSchedule operation.");
        bookingFinancesGroup.MapPost("/schedules/{scheduleId:guid}/approve", ApproveBookingPaymentSchedule).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("ApproveBookingPaymentSchedule", "Executes ApproveBookingPaymentSchedule with a required idempotency key and revision precondition.", "ApproveBookingPaymentSchedule operation.");
        bookingFinancesGroup.MapPost("/quotes", DraftBookingPaymentQuote).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("DraftBookingPaymentQuote", "Executes DraftBookingPaymentQuote with a required idempotency key and revision precondition.", "DraftBookingPaymentQuote operation.");
        bookingFinancesGroup.MapPost("/quotes/{quoteId:guid}/approve", ApproveBookingPaymentQuote).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("ApproveBookingPaymentQuote", "Executes ApproveBookingPaymentQuote with a required idempotency key and revision precondition.", "ApproveBookingPaymentQuote operation.");
        bookingFinancesGroup.MapPost("/receipts", RecordBookingReceipt).RequireAuthorization(AdminAuthorization.PaymentWrite)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("RecordBookingReceipt", "Executes RecordBookingReceipt with a required idempotency key and revision precondition.", "RecordBookingReceipt operation.");
        bookingFinancesGroup.MapPost("/receipts/{receiptId:guid}/reconciliations", DraftBookingReceiptReconciliation).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("DraftBookingReceiptReconciliation", "Executes DraftBookingReceiptReconciliation with a required idempotency key and revision precondition.", "DraftBookingReceiptReconciliation operation.");
        bookingFinancesGroup.MapPost("/reconciliations/{reconciliationId:guid}/approve", ApproveBookingReceiptReconciliation).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("ApproveBookingReceiptReconciliation", "Executes ApproveBookingReceiptReconciliation with a required idempotency key and revision precondition.", "ApproveBookingReceiptReconciliation operation.");
        bookingFinancesGroup.MapPost("/receipts/{receiptId:guid}/allocations", AllocateBookingPayment).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("AllocateBookingPayment", "Executes AllocateBookingPayment with a required idempotency key and revision precondition.", "AllocateBookingPayment operation.");
        bookingFinancesGroup.MapPost("/allocation-releases", ReleaseBookingPaymentAllocations).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("ReleaseBookingPaymentAllocations", "Executes ReleaseBookingPaymentAllocations with a required idempotency key and revision precondition.", "ReleaseBookingPaymentAllocations operation.");
        bookingFinancesGroup.MapPost("/receipts/{receiptId:guid}/reversals", ReverseBookingReceipt).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("ReverseBookingReceipt", "Executes ReverseBookingReceipt with a required idempotency key and revision precondition.", "ReverseBookingReceipt operation.");
        bookingFinancesGroup.MapPost("/receipts/{receiptId:guid}/refunds", DraftBookingPaymentRefund).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("DraftBookingPaymentRefund", "Executes DraftBookingPaymentRefund with a required idempotency key and revision precondition.", "DraftBookingPaymentRefund operation.");
        bookingFinancesGroup.MapPost("/refunds/{refundId:guid}/approve", ApproveBookingPaymentRefund).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("ApproveBookingPaymentRefund", "Executes ApproveBookingPaymentRefund with a required idempotency key and revision precondition.", "ApproveBookingPaymentRefund operation.");
        bookingFinancesGroup.MapPost("/refunds/{refundId:guid}/corrections", CorrectBookingPaymentRefund).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("CorrectBookingPaymentRefund", "Executes CorrectBookingPaymentRefund with a required idempotency key and revision precondition.", "CorrectBookingPaymentRefund operation.");
        bookingFinancesGroup.MapPost("/records/{targetId:guid}/review", ReviewBookingFinancialRecord).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("ReviewBookingFinancialRecord", "Executes ReviewBookingFinancialRecord with a required idempotency key and revision precondition.", "ReviewBookingFinancialRecord operation.");
        bookingFinancesGroup.MapPost("/records/{targetId:guid}/withdraw-approval", WithdrawBookingFinancialApproval).RequireAuthorization(AdminAuthorization.PaymentManage)
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

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> DraftBookingPaymentSchedule(Guid bookingId, [FromBody] CreatePaymentScheduleDto input, [FromServices] ISender sender, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new DraftBookingPaymentScheduleCommand(bookingId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> ApproveBookingPaymentSchedule(Guid bookingId, Guid scheduleId, [FromBody] ApproveFinancialRecordDto input, [FromServices] ISender sender, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new ApproveBookingPaymentScheduleCommand(bookingId, scheduleId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> DraftBookingPaymentQuote(Guid bookingId, [FromBody] CreatePaymentQuoteDto input, [FromServices] ISender sender, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new DraftBookingPaymentQuoteCommand(bookingId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> ApproveBookingPaymentQuote(Guid bookingId, Guid quoteId, [FromBody] ApproveFinancialRecordDto input, [FromServices] ISender sender, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new ApproveBookingPaymentQuoteCommand(bookingId, quoteId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> RecordBookingReceipt(Guid bookingId, [FromBody] CreateBookingReceiptDto input, [FromServices] ISender sender, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new RecordBookingReceiptCommand(bookingId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> DraftBookingReceiptReconciliation(Guid bookingId, Guid receiptId, [FromBody] CreateReceiptReconciliationDto input, [FromServices] ISender sender, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new DraftBookingReceiptReconciliationCommand(bookingId, receiptId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> ApproveBookingReceiptReconciliation(Guid bookingId, Guid reconciliationId, [FromBody] ApproveFinancialRecordDto input, [FromServices] ISender sender, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new ApproveBookingReceiptReconciliationCommand(bookingId, reconciliationId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> AllocateBookingPayment(Guid bookingId, Guid receiptId, [FromBody] AllocatePaymentCreditDto input, [FromServices] ISender sender, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new AllocateBookingPaymentCommand(bookingId, receiptId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> ReleaseBookingPaymentAllocations(Guid bookingId, [FromBody] AllocatePaymentCreditDto input, [FromServices] ISender sender, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new ReleaseBookingPaymentAllocationsCommand(bookingId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> ReverseBookingReceipt(Guid bookingId, Guid receiptId, [FromBody] ApproveFinancialRecordDto input, [FromServices] ISender sender, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new ReverseBookingReceiptCommand(bookingId, receiptId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> DraftBookingPaymentRefund(Guid bookingId, Guid receiptId, [FromBody] CreatePaymentRefundDto input, [FromServices] ISender sender, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new DraftBookingPaymentRefundCommand(bookingId, receiptId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> ApproveBookingPaymentRefund(Guid bookingId, Guid refundId, [FromBody] ApproveFinancialRecordDto input, [FromServices] ISender sender, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new ApproveBookingPaymentRefundCommand(bookingId, refundId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> CorrectBookingPaymentRefund(Guid bookingId, Guid refundId, [FromBody] ApproveFinancialRecordDto input, [FromServices] ISender sender, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new CorrectBookingPaymentRefundCommand(bookingId, refundId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> ReviewBookingFinancialRecord(Guid bookingId, Guid targetId, [FromBody] ReviewFinancialRecordDto input, [FromServices] ISender sender, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new ReviewBookingFinancialRecordCommand(bookingId, targetId, input, actorId, key.Value), ct);
        return Respond(bookingId, result);
    }

    private static async Task<Results<Ok<GetPaymentCommandResultDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> WithdrawBookingFinancialApproval(Guid bookingId, Guid targetId, [FromBody] ApproveFinancialRecordDto input, [FromServices] ISender sender, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new WithdrawBookingFinancialApprovalCommand(bookingId, targetId, input, actorId, key.Value), ct);
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
