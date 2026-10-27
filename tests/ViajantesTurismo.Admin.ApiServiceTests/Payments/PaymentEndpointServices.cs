using Microsoft.Extensions.DependencyInjection.Extensions;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

/// <summary>Registers payment fakes for endpoint boundary tests.</summary>
internal static class PaymentEndpointServices
{
    /// <summary>Replaces payment stores and services with the transaction, queries and empty stores.</summary>
    public static void Configure(IServiceCollection services, PaymentEndpointTransaction transaction, PaymentAccountQueryProbe queries)
    {
        services.RemoveAll<IPaymentTransaction>();
        services.AddSingleton<IPaymentTransaction>(transaction);
        services.RemoveAll<IPaymentHistoryStore>();
        services.AddSingleton<IPaymentHistoryStore>(transaction);
        services.RemoveAll<IPaymentInstructionStore>();
        services.AddSingleton<IPaymentInstructionStore, EmptyPaymentInstructionStore>();
        services.RemoveAll<IPaymentAccountStore>();
        services.AddSingleton<IPaymentAccountStore, EmptyPaymentAccountStore>();
        services.RemoveAll<IPaymentAccountQueryService>();
        services.AddSingleton<IPaymentAccountQueryService>(queries);
        services.RemoveAll<ITourStore>();
        services.AddSingleton<ITourStore, FakeTourStore>();
    }
}
