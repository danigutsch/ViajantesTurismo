namespace ViajantesTurismo.Admin.Infrastructure.Payments;

/// <summary>Persistence row for a protected payment instruction snapshot.</summary>
internal sealed class PaymentInstructionRow
{
    /// <summary>The document identifier.</summary>
    public Guid DocumentId
    {
        get; set;
    }
    /// <summary>The booking identifier.</summary>
    public Guid BookingId
    {
        get; set;
    }
    /// <summary>The payment account identifier.</summary>
    public Guid AccountId
    {
        get; set;
    }
    /// <summary>The account revision identifier.</summary>
    public Guid AccountRevisionId
    {
        get; set;
    }
    /// <summary>The document revision number.</summary>
    public int Revision
    {
        get; set;
    }
    /// <summary>The protected serialized snapshot.</summary>
    public byte[] ProtectedSnapshot { get; set; } = [];
}
