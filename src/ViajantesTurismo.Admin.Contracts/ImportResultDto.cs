namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// Result of a customer import operation.
/// </summary>
public sealed record ImportResultDto(
    int SuccessCount,
    int ErrorCount,
    IReadOnlyList<ImportConflictDto>? Conflicts = null);
