namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Declares equality or membership in a bounded set of values of a text column.</summary>
/// <param name="Column">The filtered column.</param>
/// <param name="Values">The distinct values that make a row eligible for indexing.</param>
internal sealed record MigrationOnlineIndexTextFilter(string Column, string[] Values);
