using Microsoft.Extensions.DependencyInjection.Extensions;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

internal static class PaymentEndpointServices
{
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
