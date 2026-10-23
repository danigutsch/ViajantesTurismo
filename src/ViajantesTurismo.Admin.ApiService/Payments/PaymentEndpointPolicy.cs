namespace ViajantesTurismo.Admin.ApiService.Payments;

internal static class PaymentEndpointPolicy
{
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
