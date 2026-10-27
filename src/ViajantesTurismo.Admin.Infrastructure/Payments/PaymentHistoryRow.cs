namespace ViajantesTurismo.Admin.Infrastructure.Payments;

/// <summary>Persistence row for a booking financial history with its encrypted state.</summary>
internal sealed class PaymentHistoryRow
{
    /// <summary>The booking identifier.</summary>
    public Guid BookingId
    {
        get; set;
    }
    /// <summary>The optimistic concurrency version.</summary>
    public long Version
    {
        get; set;
    }
    /// <summary>The protected serialized history state.</summary>
    public byte[] ProtectedState { get; set; } = [];
}
