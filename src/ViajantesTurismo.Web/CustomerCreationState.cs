using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Web;

internal sealed class CustomerCreationState
{
    public int CurrentStep { get; private set; } = 1;

    public PersonalInfoStepDto? PersonalInfo { get; private set; }
    public IdentificationInfoStepDto? IdentificationInfo { get; private set; }
    public ContactInfoStepDto? ContactInfo { get; private set; }
    public AddressStepDto? Address { get; private set; }
    public PhysicalInfoStepDto? PhysicalInfo { get; private set; }
    public AccommodationPreferencesStepDto? AccommodationPreferences { get; private set; }
    public EmergencyContactStepDto? EmergencyContact { get; private set; }
    public MedicalInfoStepDto? MedicalInfo { get; private set; }

    public bool IsStepCompleted(int step) => step switch
    {
        1 => PersonalInfo is not null,
        2 => IdentificationInfo is not null,
        3 => ContactInfo is not null,
        4 => Address is not null,
        5 => PhysicalInfo is not null,
        6 => AccommodationPreferences is not null,
        7 => EmergencyContact is not null,
        8 => MedicalInfo is not null,
        _ => false
    };

    /// <summary>
    /// Checks if all steps in the customer creation process have been completed.
    /// </summary>
    /// <returns>True if all step data properties are not null; otherwise, false.</returns>
    public bool IsComplete() =>
        PersonalInfo is not null &&
        IdentificationInfo is not null &&
        ContactInfo is not null &&
        Address is not null &&
        PhysicalInfo is not null &&
        AccommodationPreferences is not null &&
        EmergencyContact is not null &&
        MedicalInfo is not null;

    /// <summary>
    /// Resets the customer creation state, clearing all step data and setting the current step back to 1.
    /// </summary>
    public void Reset()
    {
        CurrentStep = 1;
        PersonalInfo = null;
        IdentificationInfo = null;
        ContactInfo = null;
        Address = null;
        PhysicalInfo = null;
        AccommodationPreferences = null;
        EmergencyContact = null;
        MedicalInfo = null;
    }
}
