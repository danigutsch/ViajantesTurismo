using PandaBear.AspNetCore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiService.Payments;

internal static class PaymentAccountEndpoints
{
    public static void MapPaymentAccountEndpoints(this WebApplication app)
    {
        var group = app.MapPaymentAccountsGroup().WithPaymentWorkflowPolicy();
        group.MapGet("/", List).RequireAuthorization(AdminAuthorization.PaymentRead)
            .WithAdminMetadata("ListPaymentAccounts", "Lists masked recipient accounts.", "Lists masked payment accounts.");
        group.MapGet("/{id:guid}", GetById).RequireAuthorization(AdminAuthorization.PaymentRead)
            .WithAdminMetadata("GetPaymentAccount", "Retrieves masked account and revision metadata.", "Retrieves a masked payment account.");
        group.MapGet("/{id:guid}/recipient", GetRecipient).RequireAuthorization(AdminAuthorization.PaymentManage)
            .WithAdminMetadata("GetPaymentAccountRecipient", "Audits Admin access to full recipient details.", "Retrieves sensitive recipient details.");
        group.MapPost("/", CreatePaymentAccount).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("CreatePaymentAccount", "Executes the revision-preconditioned CreatePaymentAccount operation.", "CreatePaymentAccount operation.");
        group.MapPut("/{id:guid}", UpdatePaymentAccount).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("UpdatePaymentAccount", "Executes the revision-preconditioned UpdatePaymentAccount operation.", "UpdatePaymentAccount operation.");
        group.MapPost("/{id:guid}/verify", VerifyPaymentAccount).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("VerifyPaymentAccount", "Executes the revision-preconditioned VerifyPaymentAccount operation.", "VerifyPaymentAccount operation.");
        group.MapPost("/{id:guid}/review", ReviewPaymentAccount).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("ReviewPaymentAccount", "Executes the revision-preconditioned ReviewPaymentAccount operation.", "ReviewPaymentAccount operation.");
        group.MapPost("/{id:guid}/retire", RetirePaymentAccount).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("RetirePaymentAccount", "Executes the revision-preconditioned RetirePaymentAccount operation.", "RetirePaymentAccount operation.");
        group.MapPost("/{id:guid}/withdraw-verification", WithdrawPaymentAccountVerification).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("WithdrawPaymentAccountVerification", "Executes the revision-preconditioned WithdrawPaymentAccountVerification operation.", "WithdrawPaymentAccountVerification operation.");
    }

    private static async Task<Ok<IReadOnlyList<GetPaymentAccountDto>>> List([FromServices] IPaymentAccountQueryService queries, CancellationToken ct) =>
        TypedResults.Ok(await queries.List(ct));

    private static async Task<Results<Ok<GetPaymentAccountDto>, NotFound>> GetById(Guid id, [FromServices] IPaymentAccountQueryService queries, CancellationToken ct)
    {
        var account = await queries.GetById(id, ct);
        return account is null ? TypedResults.NotFound() : TypedResults.Ok(account);
    }

    private static async Task<Results<Ok<GetPaymentAccountDetailsDto>, NotFound, UnauthorizedHttpResult>> GetRecipient(Guid id, [FromServices] IPaymentAccountQueryService queries, HttpContext context, CancellationToken ct)
    {
        var actorId = context.User.GetSubjectId();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return TypedResults.Unauthorized();
        }
        var details = await queries.GetSensitiveDetails(id, actorId, ct);
        return details is null ? TypedResults.NotFound() : TypedResults.Ok(details);
    }

    private static async Task<Results<Ok<GetPaymentAccountDto>, Created<GetPaymentAccountDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> CreatePaymentAccount([FromBody] CreatePaymentAccountDto input, [FromServices] CreatePaymentAccountCommandHandler handler, [FromServices] IPaymentAccountQueryService queries, HttpContext context, CancellationToken ct)
    {
        var actorId = context.User.GetSubjectId();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return TypedResults.Unauthorized();
        }
        var result = await handler.Handle(new CreatePaymentAccountCommand(input, actorId, context.Request.Headers[ContractConstants.IdempotencyKeyHeaderName].ToString()), ct);
        return await Respond(result, queries, true, ct);
    }

    private static async Task<Results<Ok<GetPaymentAccountDto>, Created<GetPaymentAccountDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> UpdatePaymentAccount(Guid id, [FromBody] UpdatePaymentAccountDto input, [FromServices] UpdatePaymentAccountCommandHandler handler, [FromServices] IPaymentAccountQueryService queries, HttpContext context, CancellationToken ct)
    {
        var actorId = context.User.GetSubjectId();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return TypedResults.Unauthorized();
        }
        var result = await handler.Handle(new UpdatePaymentAccountCommand(id, input, actorId, context.Request.Headers[ContractConstants.IdempotencyKeyHeaderName].ToString()), ct);
        return await Respond(result, queries, false, ct);
    }

    private static async Task<Results<Ok<GetPaymentAccountDto>, Created<GetPaymentAccountDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> VerifyPaymentAccount(Guid id, [FromBody] VerifyPaymentAccountDto input, [FromServices] VerifyPaymentAccountCommandHandler handler, [FromServices] IPaymentAccountQueryService queries, HttpContext context, CancellationToken ct)
    {
        var actorId = context.User.GetSubjectId();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return TypedResults.Unauthorized();
        }
        var result = await handler.Handle(new VerifyPaymentAccountCommand(id, input, actorId, context.Request.Headers[ContractConstants.IdempotencyKeyHeaderName].ToString()), ct);
        return await Respond(result, queries, false, ct);
    }

    private static async Task<Results<Ok<GetPaymentAccountDto>, Created<GetPaymentAccountDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> ReviewPaymentAccount(Guid id, [FromBody] ReviewPaymentAccountDto input, [FromServices] ReviewPaymentAccountCommandHandler handler, [FromServices] IPaymentAccountQueryService queries, HttpContext context, CancellationToken ct)
    {
        var actorId = context.User.GetSubjectId();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return TypedResults.Unauthorized();
        }
        var result = await handler.Handle(new ReviewPaymentAccountCommand(id, input, actorId, context.Request.Headers[ContractConstants.IdempotencyKeyHeaderName].ToString()), ct);
        return await Respond(result, queries, false, ct);
    }

    private static async Task<Results<Ok<GetPaymentAccountDto>, Created<GetPaymentAccountDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> RetirePaymentAccount(Guid id, [FromBody] RetirePaymentAccountDto input, [FromServices] RetirePaymentAccountCommandHandler handler, [FromServices] IPaymentAccountQueryService queries, HttpContext context, CancellationToken ct)
    {
        var actorId = context.User.GetSubjectId();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return TypedResults.Unauthorized();
        }
        var result = await handler.Handle(new RetirePaymentAccountCommand(id, input, actorId, context.Request.Headers[ContractConstants.IdempotencyKeyHeaderName].ToString()), ct);
        return await Respond(result, queries, false, ct);
    }

    private static async Task<Results<Ok<GetPaymentAccountDto>, Created<GetPaymentAccountDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> WithdrawPaymentAccountVerification(Guid id, [FromBody] WithdrawPaymentAccountVerificationDto input, [FromServices] WithdrawPaymentAccountVerificationCommandHandler handler, [FromServices] IPaymentAccountQueryService queries, HttpContext context, CancellationToken ct)
    {
        var actorId = context.User.GetSubjectId();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return TypedResults.Unauthorized();
        }
        var result = await handler.Handle(new WithdrawPaymentAccountVerificationCommand(id, input, actorId, context.Request.Headers[ContractConstants.IdempotencyKeyHeaderName].ToString()), ct);
        return await Respond(result, queries, false, ct);
    }

    private static async Task<Results<Ok<GetPaymentAccountDto>, Created<GetPaymentAccountDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> Respond(Result<Guid> result, IPaymentAccountQueryService queries, bool created, CancellationToken ct)
    {
        if (result.IsFailure)
        {
            return result.Status switch
            {
                ResultStatus.NotFound => result.ToNotFound(),
                ResultStatus.Conflict => result.ToConflict(),
                ResultStatus.Invalid => result.ToValidationProblem(),
                _ => throw new InvalidOperationException("Unexpected payment account result.")
            };
        }
        var account = await queries.GetById(result.Value, ct);
        if (account is null)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Payment account not found.", Status = StatusCodes.Status404NotFound });
        }
        return created ? TypedResults.Created("/api/v1/payment-accounts/" + account.Id, account) : TypedResults.Ok(account);
    }
}
