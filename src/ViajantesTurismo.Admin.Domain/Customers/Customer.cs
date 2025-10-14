using ViajantesTurismo.Common;

namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Represents a customer entity.
/// </summary>
public sealed class Customer : Entity<int>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Customer"/> class.
    /// </summary>
    /// <param name="personalInfo">The personal information.</param>
    /// <param name="identificationInfo">The identification information.</param>
    /// <param name="contactInfo">The contact information.</param>
    /// <param name="address">The physical address.</param>
    /// <param name="physicalInfo">The physical characteristics and bike preferences.</param>
    /// <param name="accommodationPreferences">The accommodation preferences.</param>
    /// <param name="emergencyContact">The emergency contact information.</param>
    /// <param name="medicalInfo">The medical information and allergies.</param>
    public Customer(PersonalInfo personalInfo,
        IdentificationInfo identificationInfo,
        ContactInfo contactInfo,
        Address address,
        PhysicalInfo physicalInfo,
        AccommodationPreferences accommodationPreferences,
        EmergencyContact emergencyContact,
        MedicalInfo medicalInfo)
    {
        PersonalInfo = personalInfo;
        IdentificationInfo = identificationInfo;
        ContactInfo = contactInfo;
        Address = address;
        PhysicalInfo = physicalInfo;
        AccommodationPreferences = accommodationPreferences;
        EmergencyContact = emergencyContact;
        MedicalInfo = medicalInfo;
    }

    /// <summary>Personal information.</summary>
    public PersonalInfo PersonalInfo { get; private set; }

    /// <summary>Identification information.</summary>
    public IdentificationInfo IdentificationInfo { get; private set; }

    /// <summary>Contact information.</summary>
    public ContactInfo ContactInfo { get; private set; }

    /// <summary>Physical address.</summary>
    public Address Address { get; private set; }

    /// <summary>Physical characteristics and bike preferences.</summary>
    public PhysicalInfo PhysicalInfo { get; private set; }

    /// <summary>Accommodation preferences.</summary>
    public AccommodationPreferences AccommodationPreferences { get; private set; }

    /// <summary>Emergency contact information.</summary>
    public EmergencyContact EmergencyContact { get; private set; }

    /// <summary>Medical information and allergies.</summary>
    public MedicalInfo MedicalInfo { get; private set; }

    /// <summary>
    /// Updates the customer with new information.
    /// </summary>
    /// <param name="personalInfo">The personal information.</param>
    /// <param name="identificationInfo">The identification information.</param>
    /// <param name="contactInfo">The contact information.</param>
    /// <param name="address">The physical address.</param>
    /// <param name="physicalInfo">The physical characteristics and bike preferences.</param>
    /// <param name="accommodationPreferences">The accommodation preferences.</param>
    /// <param name="emergencyContact">The emergency contact information.</param>
    /// <param name="medicalInfo">The medical information and allergies.</param>
    public void Update(PersonalInfo personalInfo,
        IdentificationInfo identificationInfo,
        ContactInfo contactInfo,
        Address address,
        PhysicalInfo physicalInfo,
        AccommodationPreferences accommodationPreferences,
        EmergencyContact emergencyContact,
        MedicalInfo medicalInfo)
    {
        PersonalInfo = personalInfo;
        IdentificationInfo = identificationInfo;
        ContactInfo = contactInfo;
        Address = address;
        PhysicalInfo = physicalInfo;
        AccommodationPreferences = accommodationPreferences;
        EmergencyContact = emergencyContact;
        MedicalInfo = medicalInfo;
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
#pragma warning disable CS8618
    private Customer()
    {
    }
}