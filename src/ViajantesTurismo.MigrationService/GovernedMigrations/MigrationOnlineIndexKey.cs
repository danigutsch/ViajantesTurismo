namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Declares an ordered B-tree key made from a column or a JSON text property.</summary>
/// <param name="Column">The indexed column.</param>
/// <param name="JsonProperty">The optional top-level JSON property to extract as text.</param>
internal sealed record MigrationOnlineIndexKey(string Column, string? JsonProperty = null);
