namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>A row with an integer key that EF leaves to the store.</summary>
internal sealed class StoreKeyedRow
{
    /// <summary>Gets or sets the store-generated key.</summary>
    public int Id
    {
        get; set;
    }
}
