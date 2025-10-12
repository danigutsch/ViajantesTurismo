using ViajantesTurismo.Common;

namespace ViajantesTurismo.Admin.Domain;

/// <summary>
/// Represents a customer entity.
/// </summary>
public sealed class Customer : Entity<int>
{
    /// <summary>Personal information.</summary>
    public required PersonalInfo PersonalInfo { get; init; }

    /// <summary>Identification information.</summary>
    public required IdentificationInfo IdentificationInfo { get; init; }

    /// <summary>Contact information.</summary>
    public required ContactInfo ContactInfo { get; init; }

    /// <summary>Physical address.</summary>
    public required Address Address { get; init; }

    /// <summary>Physical characteristics and bike preferences.</summary>
    public required PhysicalInfo PhysicalInfo { get; init; }

    /// <summary>Accommodation preferences.</summary>
    public required AccommodationPreferences AccommodationPreferences { get; init; }

    /// <summary>Emergency contact information.</summary>
    public required EmergencyContact EmergencyContact { get; init; }

    /// <summary>Medical information and allergies.</summary>
    public required MedicalInfo MedicalInfo { get; init; }
}
