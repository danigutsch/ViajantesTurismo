using SharedKernel.AspNetCore.Authentication.Bff;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

/// <summary>Composes the Management Web payment workflows.</summary>
internal static class PaymentWorkflowServiceCollectionExtensions
{
    /// <summary>Registers the booking payment, payment instruction, and payment account API clients.</summary>
    internal static void AddPaymentWorkflows(this IServiceCollection services)
    {
        services.AddHttpClient<IBookingPaymentsApiClient, BookingPaymentsApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
            .AddTokenExchangeHandler(ApiAudienceNames.Admin, scope: ApiAudienceNames.Admin);
        services.AddHttpClient(nameof(IPaymentInstructionsApiClient), client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
            .AddTypedClient<IPaymentInstructionsApiClient>(PaymentInstructionsApiClient.WithLatestReviewOnly)
            .AddTokenExchangeHandler(ApiAudienceNames.Admin, scope: ApiAudienceNames.Admin);
        services.AddHttpClient<IPaymentAccountsApiClient, PaymentAccountsApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.Api}"))
            .AddTokenExchangeHandler(ApiAudienceNames.Admin, scope: ApiAudienceNames.Admin);
    }
}
