namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Defines the raw profile values used to create or revise a customer.
/// </summary>
/// <param name="PersonalInfo">The requested personal information.</param>
/// <param name="IdentificationInfo">The requested identification information.</param>
/// <param name="ContactInfo">The requested contact information.</param>
/// <param name="Address">The requested physical address.</param>
/// <param name="PhysicalInfo">The requested physical characteristics and bike preference.</param>
/// <param name="AccommodationPreferences">The requested accommodation preferences.</param>
/// <param name="HealthInfo">The requested emergency contact and medical information.</param>
public sealed record CustomerDefinition(
    PersonalInfoDefinition PersonalInfo,
    IdentificationInfoDefinition IdentificationInfo,
    ContactInfoDefinition ContactInfo,
    AddressDefinition Address,
    PhysicalInfoDefinition PhysicalInfo,
    AccommodationPreferencesDefinition AccommodationPreferences,
    CustomerHealthDefinition HealthInfo);
