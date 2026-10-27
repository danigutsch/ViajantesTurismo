using ViajantesTurismo.Admin.Domain.PaymentAccounts;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

/// <summary>Persistence row for a payment account with its encrypted state.</summary>
internal sealed class PaymentAccountRow
{
    /// <summary>The account identifier.</summary>
    public Guid Id
    {
        get; set;
    }
    /// <summary>The optimistic concurrency version.</summary>
    public long Version
    {
        get; set;
    }
    /// <summary>The account lifecycle status.</summary>
    public PaymentAccountStatus Status
    {
        get; set;
    }
    /// <summary>The current revision number.</summary>
    public int Revision
    {
        get; set;
    }
    /// <summary>The protected serialized account state.</summary>
    public byte[] ProtectedState { get; set; } = [];
}
