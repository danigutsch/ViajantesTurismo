using JetBrains.Annotations;

namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Represents medical information and allergies.
/// </summary>
public sealed class MedicalInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MedicalInfo"/> class.
    /// </summary>
    /// <param name="allergies">The allergies.</param>
    /// <param name="additionalInfo">The additional medical information.</param>
    public MedicalInfo(string? allergies, string? additionalInfo)
    {
        Allergies = allergies;
        AdditionalInfo = additionalInfo;
    }

    /// <summary>Allergies.</summary>
    public string? Allergies { get; private set; }

    /// <summary>Additional medical information.</summary>
    public string? AdditionalInfo { get; private set; }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
#pragma warning disable CS8618
    [UsedImplicitly]
    private MedicalInfo()
    {
    }
}
