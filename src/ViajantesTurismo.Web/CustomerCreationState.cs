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

    /// <summary>
    /// Sets the personal information for the customer and advances to the next step.
    /// </summary>
    /// <param name="dto">The personal information data.</param>
    public void SetPersonalInfo(PersonalInfoStepDto dto)
    {
        PersonalInfo = dto;
        CurrentStep = Math.Max(CurrentStep, 2);
    }

    /// <summary>
    /// Sets the identification information for the customer and advances to the next step.
    /// </summary>
    /// <param name="dto">The identification information data.</param>
    public void SetIdentificationInfo(IdentificationInfoStepDto dto)
    {
        IdentificationInfo = dto;
        CurrentStep = Math.Max(CurrentStep, 3);
    }

    /// <summary>
    /// Sets the contact information for the customer and advances to the next step.
    /// </summary>
    /// <param name="dto">The contact information data.</param>
    public void SetContactInfo(ContactInfoStepDto dto)
    {
        ContactInfo = dto;
        CurrentStep = Math.Max(CurrentStep, 4);
    }

    /// <summary>
    /// Sets the address information for the customer and advances to the next step.
    /// </summary>
    /// <param name="dto">The address information data.</param>
    public void SetAddress(AddressStepDto dto)
    {
        Address = dto;
        CurrentStep = Math.Max(CurrentStep, 5);
    }

    /// <summary>
    /// Sets the physical information for the customer and advances to the next step.
    /// </summary>
    /// <param name="dto">The physical information data.</param>
    public void SetPhysicalInfo(PhysicalInfoStepDto dto)
    {
        PhysicalInfo = dto;
        CurrentStep = Math.Max(CurrentStep, 6);
    }

    /// <summary>
    /// Sets the accommodation preferences for the customer and advances to the next step.
    /// </summary>
    /// <param name="dto">The accommodation preferences data.</param>
    public void SetAccommodationPreferences(AccommodationPreferencesStepDto dto)
    {
        AccommodationPreferences = dto;
        CurrentStep = Math.Max(CurrentStep, 7);
    }

    /// <summary>
    /// Sets the emergency contact information for the customer and advances to the next step.
    /// </summary>
    /// <param name="dto">The emergency contact information data.</param>
    public void SetEmergencyContact(EmergencyContactStepDto dto)
    {
        EmergencyContact = dto;
        CurrentStep = Math.Max(CurrentStep, 8);
    }

    /// <summary>
    /// Sets the medical information for the customer and completes the wizard.
    /// </summary>
    /// <param name="dto">The medical information data.</param>
    public void SetMedicalInfo(MedicalInfoStepDto dto)
    {
        MedicalInfo = dto;
        CurrentStep = Math.Max(CurrentStep, 8);
    }

    /// <summary>
    /// Navigates to a specific step in the wizard.
    /// </summary>
    /// <param name="step">The step number (1-8).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when step is not between 1 and 8.</exception>
    public void NavigateToStep(int step)
    {
        if (step is < 1 or > 8)
            throw new ArgumentOutOfRangeException(nameof(step), "Step must be between 1 and 8.");

        CurrentStep = step;
    }

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
