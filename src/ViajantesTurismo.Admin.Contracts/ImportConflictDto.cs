namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// Represents a single row where the incoming CSV email matches an existing customer in the database.
/// </summary>
public sealed record ImportConflictDto(string Email);
