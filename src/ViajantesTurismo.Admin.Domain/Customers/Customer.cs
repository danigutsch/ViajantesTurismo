using JetBrains.Annotations;
using PandaBear.Domain;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Represents a customer entity.
/// </summary>
[GenerateModelSupport(Identity = true)]
public sealed partial class Customer : IEntity<Guid>
{
    private Customer(
        PersonalInfo personalInfo,
        IdentificationInfo identificationInfo,
        ContactInfo contactInfo,
        Address address,
        PhysicalInfo physicalInfo,
        AccommodationPreferences accommodationPreferences,
        CustomerHealthInfo healthInfo)
    {
        Id = Guid.CreateVersion7();
        PersonalInfo = personalInfo;
        IdentificationInfo = identificationInfo;
        ContactInfo = contactInfo;
        Address = address;
        PhysicalInfo = physicalInfo;
        AccommodationPreferences = accommodationPreferences;
        EmergencyContact = healthInfo.EmergencyContact;
        MedicalInfo = healthInfo.MedicalInfo;
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
    [UsedImplicitly]
    private Customer()
    {
    }

    /// <summary>Unique customer identifier.</summary>
    public Guid Id
    {
        get; private init;
    }

    /// <summary>Personal information.</summary>
    public PersonalInfo PersonalInfo { get; private set; } = default!;

    /// <summary>Identification information.</summary>
    public IdentificationInfo IdentificationInfo { get; private set; } = default!;

    /// <summary>Contact information.</summary>
    public ContactInfo ContactInfo { get; private set; } = default!;

    /// <summary>Physical address.</summary>
    public Address Address { get; private set; } = default!;

    /// <summary>Physical characteristics and bike preferences.</summary>
    public PhysicalInfo PhysicalInfo { get; private set; } = default!;

    /// <summary>Accommodation preferences.</summary>
    public AccommodationPreferences AccommodationPreferences { get; private set; } = default!;

    /// <summary>Emergency contact information.</summary>
    public EmergencyContact EmergencyContact { get; private set; } = default!;

    /// <summary>Medical information and allergies.</summary>
    public MedicalInfo MedicalInfo { get; private set; } = default!;

    /// <summary>
    /// Creates a customer after validating every profile value.
    /// </summary>
    /// <param name="definition">The requested profile values.</param>
    /// <param name="today">The current business date, used to validate the birth date.</param>
    /// <returns>A Result containing the new customer, or every validation failure.</returns>
    public static Result<Customer> Create(CustomerDefinition definition, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var personal = definition.PersonalInfo;
        var personalInfoResult = PersonalInfo.Create(
            personal.FirstName,
            personal.LastName,
            personal.Gender,
            personal.BirthDate,
            personal.Nationality,
            personal.Occupation,
            today);
        var identificationInfoResult = IdentificationInfo.Create(
            definition.IdentificationInfo.NationalId,
            definition.IdentificationInfo.IdNationality);
        var contact = definition.ContactInfo;
        var contactInfoResult = ContactInfo.Create(contact.Email, contact.Mobile, contact.Instagram, contact.Facebook);
        var address = definition.Address;
        var addressResult = Address.Create(
            address.Street,
            address.Complement,
            address.Neighborhood,
            address.PostalCode,
            address.City,
            address.State,
            address.Country);
        var physical = definition.PhysicalInfo;
        var physicalInfoResult = PhysicalInfo.Create(physical.WeightKg, physical.HeightCentimeters, physical.BikeType);
        var health = definition.HealthInfo;
        var emergencyContactResult = EmergencyContact.Create(health.EmergencyContactName, health.EmergencyContactMobile);
        var medicalInfoResult = MedicalInfo.Create(health.Allergies, health.AdditionalInfo);
        var accommodation = definition.AccommodationPreferences;
        var accommodationPreferencesResult = AccommodationPreferences.Create(
            accommodation.RoomType,
            accommodation.BedType,
            accommodation.CompanionId);

        var errors = new ValidationErrors();
        AddIfFailure(errors, personalInfoResult);
        AddIfFailure(errors, identificationInfoResult);
        AddIfFailure(errors, contactInfoResult);
        AddIfFailure(errors, addressResult);
        AddIfFailure(errors, physicalInfoResult);
        AddIfFailure(errors, emergencyContactResult);
        AddIfFailure(errors, medicalInfoResult);
        AddIfFailure(errors, accommodationPreferencesResult);

        if (errors.HasErrors)
        {
            return errors.ToResult<Customer>();
        }

        return Result.Ok(new Customer(
            personalInfoResult.Value,
            identificationInfoResult.Value,
            contactInfoResult.Value,
            addressResult.Value,
            physicalInfoResult.Value,
            accommodationPreferencesResult.Value,
            new CustomerHealthInfo(emergencyContactResult.Value, medicalInfoResult.Value)));
    }

    /// <summary>
    /// Revises the whole customer profile at once.
    /// </summary>
    /// <remarks>
    /// Every value is validated before any change is applied, so a failure leaves the customer unchanged and
    /// reports every invalid value.
    /// </remarks>
    /// <param name="definition">The revised profile values.</param>
    /// <param name="today">The current business date, used to validate the birth date.</param>
    /// <returns>A Result indicating success or every validation failure.</returns>
    public Result Revise(CustomerDefinition definition, DateOnly today)
    {
        var revisedResult = Create(definition, today);
        if (revisedResult.IsFailure)
        {
            return revisedResult.ConvertError();
        }

        var revised = revisedResult.Value;
        PersonalInfo = revised.PersonalInfo;
        IdentificationInfo = revised.IdentificationInfo;
        ContactInfo = revised.ContactInfo;
        Address = revised.Address;
        PhysicalInfo = revised.PhysicalInfo;
        AccommodationPreferences = revised.AccommodationPreferences;
        EmergencyContact = revised.EmergencyContact;
        MedicalInfo = revised.MedicalInfo;
        return Result.Ok();
    }

    private static void AddIfFailure<T>(ValidationErrors errors, Result<T> result)
        where T : notnull
    {
        if (result.IsFailure)
        {
            errors.Add(result);
        }
    }
}
