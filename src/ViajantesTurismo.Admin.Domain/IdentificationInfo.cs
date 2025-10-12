namespace ViajantesTurismo.Admin.Domain;

/// <summary>
/// Represents identification information of a customer.
/// </summary>
public sealed class IdentificationInfo
{
    /// <summary>National ID.</summary>
    public required string NationalId { get; init; }

    /// <summary>The nationality that issued the ID.</summary>
    public required string IdNationality { get; init; }
}