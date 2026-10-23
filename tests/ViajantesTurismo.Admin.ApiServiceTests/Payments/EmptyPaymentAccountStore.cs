using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

internal sealed class EmptyPaymentAccountStore : IPaymentAccountStore
{
    public Task<PaymentAccount?> GetById(Guid id, CancellationToken ct) => Task.FromResult<PaymentAccount?>(null);
    public Task<IReadOnlyList<PaymentAccount>> List(CancellationToken ct) => Task.FromResult<IReadOnlyList<PaymentAccount>>([]);
    public Task Save(PaymentAccount account, CancellationToken ct) => throw new InvalidOperationException("Unexpected persistence in HTTP boundary test.");
    public void AuditAccess(PaymentAccount account, string actorId, DateTimeOffset now) => throw new InvalidOperationException("Unexpected audit in HTTP boundary test.");
}
