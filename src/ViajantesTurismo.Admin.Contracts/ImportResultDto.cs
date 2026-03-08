namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// Result of a customer import operation.
/// </summary>
public sealed record ImportResultDto(
    int SuccessCount,
    int ErrorCount,
    IReadOnlyList<ImportConflictDto>? Conflicts = null,
    IReadOnlyList<ImportSuccessRowDto>? SuccessRows = null,
    IReadOnlyList<ImportErrorRowDto>? ErrorRows = null);

/// <summary>
/// Represents one successful row in an import summary.
/// </summary>
/// <param name="Email">Email associated with the row.</param>
/// <param name="Outcome">Row outcome (for example, created or updated).</param>
/// <param name="CustomerId">Identifier of the resulting customer when available.</param>
public sealed record ImportSuccessRowDto(
    string Email,
    string Outcome,
    Guid? CustomerId = null);

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
