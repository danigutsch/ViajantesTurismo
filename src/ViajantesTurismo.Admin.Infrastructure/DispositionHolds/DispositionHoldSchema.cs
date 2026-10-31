namespace ViajantesTurismo.Admin.Infrastructure.DispositionHolds;

/// <summary>Names the Admin schema that holds only the disposition hold table.</summary>
internal static class DispositionHoldSchema
{
    /// <summary>The schema name; a dedicated schema keeps hold grants narrow.</summary>
    public const string Name = "disposition_hold";

    /// <summary>The hold table name inside <see cref="Name" />.</summary>
    public const string HoldsTable = "holds";
}
