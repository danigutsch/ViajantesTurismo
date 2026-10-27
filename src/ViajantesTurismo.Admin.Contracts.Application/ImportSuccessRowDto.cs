namespace ViajantesTurismo.Admin.Contracts.Application;

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
