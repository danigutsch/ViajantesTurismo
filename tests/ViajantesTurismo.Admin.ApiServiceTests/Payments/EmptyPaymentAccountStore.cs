using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

/// <summary>Payment account store with no accounts that fails on any write or audit.</summary>
internal sealed class EmptyPaymentAccountStore : IPaymentAccountStore
{
    /// <summary>Returns no account.</summary>
    public Task<PaymentAccount?> GetById(Guid id, CancellationToken ct) => Task.FromResult<PaymentAccount?>(null);
    /// <summary>Returns no accounts.</summary>
    public Task<IReadOnlyList<PaymentAccount>> List(CancellationToken ct) => Task.FromResult<IReadOnlyList<PaymentAccount>>([]);
    /// <summary>Always throws because the HTTP boundary test must not persist.</summary>
    public Task Save(PaymentAccount account, CancellationToken ct) => throw new InvalidOperationException("Unexpected persistence in HTTP boundary test.");
    /// <summary>Always throws because the HTTP boundary test must not audit.</summary>
    public void AuditAccess(PaymentAccount account, string actorId, DateTimeOffset now) => throw new InvalidOperationException("Unexpected audit in HTTP boundary test.");
}
