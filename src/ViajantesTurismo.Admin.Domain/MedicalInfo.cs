namespace ViajantesTurismo.Admin.Domain;

/// <summary>
/// Represents medical information and allergies.
/// </summary>
public sealed class MedicalInfo
{
    /// <summary>Allergies.</summary>
    public string? Allergies { get; init; }

    /// <summary>Additional medical information.</summary>
    public string? AdditionalInfo { get; init; }
}