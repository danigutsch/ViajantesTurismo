namespace ViajantesTurismo.Admin.Infrastructure.Payments;

internal sealed class PaymentInstructionRow
{
    public Guid DocumentId
    {
        get; set;
    }
    public Guid BookingId
    {
        get; set;
    }
    public Guid AccountId
    {
        get; set;
    }
    public Guid AccountRevisionId
    {
        get; set;
    }
    public int Revision
    {
        get; set;
    }
    public byte[] ProtectedSnapshot { get; set; } = [];
}
