namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>
/// Result of a customer import operation.
/// </summary>
public sealed record ImportResultDto(
    int SuccessCount,
    int ErrorCount,
    IReadOnlyList<ImportConflictDto>? Conflicts = null,
    IReadOnlyList<ImportSuccessRowDto>? SuccessRows = null,
    IReadOnlyList<ImportErrorRowDto>? ErrorRows = null);
