using PandaBear.Idempotency;
using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiService.Payments;

/// <summary>Policy applied to payment workflow endpoint groups.</summary>
internal static class PaymentEndpointPolicy
{
    /// <summary>Parses the required operation key before a payment command is constructed.</summary>
    public static Result<IdempotencyKey> ReadIdempotencyKey(HttpContext context) =>
        IdempotencyKey.TryFrom(context.Request.Headers[ContractConstants.IdempotencyKeyHeaderName].ToString(), out var key)
            ? Result.Ok(key)
            : Result.Invalid<IdempotencyKey>(detail: "A valid Idempotency-Key is required.", field: ContractConstants.IdempotencyKeyHeaderName, message: "A valid Idempotency-Key is required.");

    /// <summary>Disables caching and returns not found unless <c>Payments:Enabled</c> is true.</summary>
    public static RouteGroupBuilder WithPaymentWorkflowPolicy(this RouteGroupBuilder group)
    {
        group.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            var enabled = bool.TryParse(context.HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Payments:Enabled"], out var configured) && configured;
            return enabled ? await next(context) : TypedResults.NotFound();
        });
        return group;
    }
}
