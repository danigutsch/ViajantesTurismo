namespace ViajantesTurismo.Admin.Infrastructure.Payments;

internal sealed class PaymentHistoryRow
{
    public Guid BookingId
    {
        get; set;
    }
    public long Version
    {
        get; set;
    }
    public byte[] ProtectedState { get; set; } = [];
}
