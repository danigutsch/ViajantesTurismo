namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>
/// Represents one failed row in an import summary.
/// </summary>
/// <param name="LineNumber">CSV line number of the failure.</param>
/// <param name="Field">Field name associated with the failure, if any.</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Email">Email associated with the row, if available.</param>
public sealed record ImportErrorRowDto(
    int LineNumber,
    string? Field,
    string Message,
    string? Email = null);
