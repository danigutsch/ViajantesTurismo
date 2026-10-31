namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>A row whose key is assigned by the caller.</summary>
internal sealed class ClientKeyedRow
{
    /// <summary>Gets or sets the caller-assigned key.</summary>
    public Guid Id
    {
        get; set;
    }
}
