using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Web.Models;

namespace ViajantesTurismo.Admin.Web;

internal sealed class CustomerCreationState
{
    public int CurrentStep { get; private set; } = 1;

    public PersonalInfoFormModel? PersonalInfo { get; private set; }
    public IdentificationInfoFormModel? IdentificationInfo { get; private set; }
    public ContactInfoFormModel? ContactInfo { get; private set; }
    public AddressFormModel? Address { get; private set; }
    public PhysicalInfoFormModel? PhysicalInfo { get; private set; }
    public AccommodationPreferencesFormModel? AccommodationPreferences { get; private set; }
    public EmergencyContactFormModel? EmergencyContact { get; private set; }
    public MedicalInfoFormModel? MedicalInfo { get; private set; }

    /// <summary>
    /// Sets the personal information for the customer and advances to the next step.
    /// </summary>
    /// <param name="model">The personal information form model.</param>
    public void SetPersonalInfo(PersonalInfoFormModel model)
    {
        PersonalInfo = model;
        CurrentStep = Math.Max(CurrentStep, 2);
    }

    /// <summary>
    /// Sets the identification information for the customer and advances to the next step.
    /// </summary>
    /// <param name="model">The identification information form model.</param>
    public void SetIdentificationInfo(IdentificationInfoFormModel model)
    {
        IdentificationInfo = model;
        CurrentStep = Math.Max(CurrentStep, 3);
    }

    /// <summary>
    /// Sets the contact information for the customer and advances to the next step.
    /// </summary>
    /// <param name="model">The contact information form model.</param>
    public void SetContactInfo(ContactInfoFormModel model)
    {
        ContactInfo = model;
        CurrentStep = Math.Max(CurrentStep, 4);
    }

    /// <summary>
    /// Sets the address information for the customer and advances to the next step.
    /// </summary>
    /// <param name="model">The address information form model.</param>
    public void SetAddress(AddressFormModel model)
    {
        Address = model;
        CurrentStep = Math.Max(CurrentStep, 5);
    }

    /// <summary>
    /// Sets the physical information for the customer and advances to the next step.
    /// </summary>
    /// <param name="model">The physical information form model.</param>
    public void SetPhysicalInfo(PhysicalInfoFormModel model)
    {
        PhysicalInfo = model;
        CurrentStep = Math.Max(CurrentStep, 6);
    }

    /// <summary>
    /// Sets the accommodation preferences for the customer and advances to the next step.
    /// </summary>
    /// <param name="model">The accommodation preferences form model.</param>
    public void SetAccommodationPreferences(AccommodationPreferencesFormModel model)
    {
        AccommodationPreferences = model;
        CurrentStep = Math.Max(CurrentStep, 7);
    }

    /// <summary>
    /// Sets the emergency contact information for the customer and advances to the next step.
    /// </summary>
    /// <param name="model">The emergency contact information form model.</param>
    public void SetEmergencyContact(EmergencyContactFormModel model)
    {
        EmergencyContact = model;
        CurrentStep = Math.Max(CurrentStep, 8);
    }

    /// <summary>
    /// Sets the medical information for the customer and completes the wizard.
    /// </summary>
    /// <param name="model">The medical information form model.</param>
    public void SetMedicalInfo(MedicalInfoFormModel model)
    {
        MedicalInfo = model;
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

    /// <summary>
    /// Creates a CreateCustomerDto from the completed form models.
    /// </summary>
    /// <returns>A CreateCustomerDto with all the customer information.</returns>
    /// <exception cref="InvalidOperationException">Thrown when not all steps are completed.</exception>
    public CreateCustomerDto ToCreateCustomerDto()
    {
        if (!IsComplete())
        {
            throw new InvalidOperationException("Cannot create customer DTO: not all steps are completed.");
        }

        return new CreateCustomerDto
        {
            PersonalInfo = PersonalInfo!.ToDto(),
            IdentificationInfo = IdentificationInfo!.ToDto(),
            ContactInfo = ContactInfo!.ToDto(),
            Address = Address!.ToDto(),
            PhysicalInfo = PhysicalInfo!.ToDto(),
            AccommodationPreferences = AccommodationPreferences!.ToDto(),
            EmergencyContact = EmergencyContact!.ToDto(),
            MedicalInfo = MedicalInfo!.ToDto()
        };
    }
}
