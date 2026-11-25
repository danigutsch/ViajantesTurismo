using SharedKernel.BuildingBlocks;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Loads protected recipient accounts and stages state and metadata audits in the current transaction.</summary>
public interface IPaymentAccountStore
{
    /// <summary>Loads an account; only explicitly authorized use cases may access recipient details.</summary>
    Task<PaymentAccount?> GetById(Guid id, CancellationToken ct);
    /// <summary>Loads accounts for masked staff projection.</summary>
    Task<IReadOnlyList<PaymentAccount>> List(CancellationToken ct);
    /// <summary>Loads only the requested stable page of accounts, optionally limited to verified rows.</summary>
    Task<(IReadOnlyList<PaymentAccount> Items, PageWindow Window)> ListPage(int? page, int pageSize, bool verifiedOnly, CancellationToken ct);
    /// <summary>Stages encrypted state and metadata-only events; does not commit.</summary>
    Task Save(PaymentAccount account, CancellationToken ct);
    /// <summary>Stages a sensitive-access metadata audit; does not store recipient values.</summary>
    void AuditAccess(PaymentAccount account, string actorId, DateTimeOffset now);
}
