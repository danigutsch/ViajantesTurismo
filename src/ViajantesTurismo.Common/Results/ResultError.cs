namespace ViajantesTurismo.Common.Results;

/// <summary>
/// Represents error information for a failed result.
/// </summary>
/// <param name="Detail">Specific explanation of the problem instance.</param>
/// <param name="ValidationErrors">Optional dictionary mapping field names to validation error messages.</param>
public record ResultError(
    string Detail,
    Dictionary<string, string[]>? ValidationErrors = null);