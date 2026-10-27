namespace ViajantesTurismo.Admin.ApiService.Payments;

/// <summary>Policy applied to payment workflow endpoint groups.</summary>
internal static class PaymentEndpointPolicy
{
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
