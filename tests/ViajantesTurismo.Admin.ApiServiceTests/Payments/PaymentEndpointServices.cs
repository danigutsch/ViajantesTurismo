using Microsoft.Extensions.DependencyInjection.Extensions;
using SharedKernel.Mediator;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

/// <summary>Registers payment fakes for endpoint boundary tests.</summary>
internal static class PaymentEndpointServices
{
    /// <summary>Replaces payment stores and prevents payment tests from reading booking contract sources.</summary>
    public static void Configure(IServiceCollection services, PaymentEndpointSender sender, PaymentAccountQueryProbe queries)
    {
        services.RemoveAll<ISender>();
        services.AddSingleton<ISender>(sender);
        services.RemoveAll<IPaymentInstructionStore>();
        services.AddSingleton<IPaymentInstructionStore, EmptyPaymentInstructionStore>();
        services.RemoveAll<IPaymentAccountStore>();
        services.AddSingleton<IPaymentAccountStore, EmptyPaymentAccountStore>();
        services.RemoveAll<IPaymentAccountQueryService>();
        services.AddSingleton<IPaymentAccountQueryService>(queries);
        services.RemoveAll<ITourStore>();
        services.AddSingleton<ITourStore, FakeTourStore>();
        services.RemoveAll<IDocumentSourceVersionReader>();
        services.AddSingleton<IDocumentSourceVersionReader, UnexpectedContractSourceVersionReader>();
    }
}
