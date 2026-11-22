using PandaBear.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Api.Payments;

/// <summary>Endpoints for managing payment accounts.</summary>
internal static class PaymentAccountEndpoints
{
    /// <summary>Maps the payment account endpoints under the payment workflow policy.</summary>
    public static void MapPaymentAccountEndpoints(this WebApplication app)
    {
        var paymentAccountsGroup = app.MapPaymentAccountsGroup().WithPaymentWorkflowPolicy();
        paymentAccountsGroup.MapGet("/", List).RequireAuthorization(AdminAuthorization.PaymentRead)
            .WithAdminMetadata("ListPaymentAccounts", "Lists masked recipient accounts.", "Lists masked payment accounts.");
        paymentAccountsGroup.MapGet("/{id:guid}", GetById).RequireAuthorization(AdminAuthorization.PaymentRead)
            .WithAdminMetadata("GetPaymentAccount", "Retrieves masked account and revision metadata.", "Retrieves a masked payment account.");
        paymentAccountsGroup.MapGet("/{id:guid}/recipient", GetRecipient).RequireAuthorization(AdminAuthorization.PaymentManage)
            .WithAdminMetadata("GetPaymentAccountRecipient", "Audits Admin access to full recipient details.", "Retrieves sensitive recipient details.");
        paymentAccountsGroup.MapPost("/", CreatePaymentAccount).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("CreatePaymentAccount", "Executes the revision-preconditioned CreatePaymentAccount operation.", "CreatePaymentAccount operation.");
        paymentAccountsGroup.MapPut("/{id:guid}", UpdatePaymentAccount).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("UpdatePaymentAccount", "Executes the revision-preconditioned UpdatePaymentAccount operation.", "UpdatePaymentAccount operation.");
        paymentAccountsGroup.MapPost("/{id:guid}/verify", VerifyPaymentAccount).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("VerifyPaymentAccount", "Executes the revision-preconditioned VerifyPaymentAccount operation.", "VerifyPaymentAccount operation.");
        paymentAccountsGroup.MapPost("/{id:guid}/review", ReviewPaymentAccount).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("ReviewPaymentAccount", "Executes the revision-preconditioned ReviewPaymentAccount operation.", "ReviewPaymentAccount operation.");
        paymentAccountsGroup.MapPost("/{id:guid}/retire", RetirePaymentAccount).RequireAuthorization(AdminAuthorization.PaymentManage)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .WithAdminMetadata("RetirePaymentAccount", "Executes the revision-preconditioned RetirePaymentAccount operation.", "RetirePaymentAccount operation.");
        paymentAccountsGroup.MapPost("/{id:guid}/withdraw-verification", WithdrawPaymentAccountVerification).RequireAuthorization(AdminAuthorization.PaymentManage)
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

    private static async Task<Results<Ok<GetPaymentAccountDto>, Created<GetPaymentAccountDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> CreatePaymentAccount([FromBody] CreatePaymentAccountDto input, [FromServices] ISender sender, [FromServices] IPaymentAccountQueryService queries, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new CreatePaymentAccountCommand(input, actorId, key.Value), ct);
        return await Respond(result, queries, true, ct);
    }

    private static async Task<Results<Ok<GetPaymentAccountDto>, Created<GetPaymentAccountDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> UpdatePaymentAccount(Guid id, [FromBody] UpdatePaymentAccountDto input, [FromServices] ISender sender, [FromServices] IPaymentAccountQueryService queries, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new UpdatePaymentAccountCommand(id, input, actorId, key.Value), ct);
        return await Respond(result, queries, false, ct);
    }

    private static async Task<Results<Ok<GetPaymentAccountDto>, Created<GetPaymentAccountDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> VerifyPaymentAccount(Guid id, [FromBody] VerifyPaymentAccountDto input, [FromServices] ISender sender, [FromServices] IPaymentAccountQueryService queries, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new VerifyPaymentAccountCommand(id, input, actorId, key.Value), ct);
        return await Respond(result, queries, false, ct);
    }

    private static async Task<Results<Ok<GetPaymentAccountDto>, Created<GetPaymentAccountDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> ReviewPaymentAccount(Guid id, [FromBody] ReviewPaymentAccountDto input, [FromServices] ISender sender, [FromServices] IPaymentAccountQueryService queries, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new ReviewPaymentAccountCommand(id, input, actorId, key.Value), ct);
        return await Respond(result, queries, false, ct);
    }

    private static async Task<Results<Ok<GetPaymentAccountDto>, Created<GetPaymentAccountDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> RetirePaymentAccount(Guid id, [FromBody] RetirePaymentAccountDto input, [FromServices] ISender sender, [FromServices] IPaymentAccountQueryService queries, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new RetirePaymentAccountCommand(id, input, actorId, key.Value), ct);
        return await Respond(result, queries, false, ct);
    }

    private static async Task<Results<Ok<GetPaymentAccountDto>, Created<GetPaymentAccountDto>, NotFound<ProblemDetails>, Conflict<ProblemDetails>, ValidationProblem, UnauthorizedHttpResult>> WithdrawPaymentAccountVerification(Guid id, [FromBody] WithdrawPaymentAccountVerificationDto input, [FromServices] ISender sender, [FromServices] IPaymentAccountQueryService queries, HttpContext context, CancellationToken ct)
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
        var result = await sender.Send(new WithdrawPaymentAccountVerificationCommand(id, input, actorId, key.Value), ct);
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
