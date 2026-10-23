using ViajantesTurismo.Admin.Domain.PaymentAccounts;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

internal sealed class PaymentAccountRow
{
    public Guid Id
    {
        get; set;
    }
    public long Version
    {
        get; set;
    }
    public PaymentAccountStatus Status
    {
        get; set;
    }
    public int Revision
    {
        get; set;
    }
    public byte[] ProtectedState { get; set; } = [];
}
