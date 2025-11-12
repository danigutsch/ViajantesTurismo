using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Provides predefined customer-related error results.
/// </summary>
public static class CustomerErrors
{
    /// <summary>
    /// Indicates that the first name is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<PersonalInfo> EmptyFirstName() => Result<PersonalInfo>.Invalid(
        detail: "First name is required.",
        field: "FirstName",
        message: "First name is required.");

    /// <summary>
    /// Indicates that the last name is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<PersonalInfo> EmptyLastName() => Result<PersonalInfo>.Invalid(
        detail: "Last name is required.",
        field: "LastName",
        message: "Last name is required.");

    /// <summary>
    /// Indicates that the gender is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<PersonalInfo> EmptyGender() => Result<PersonalInfo>.Invalid(
        detail: "Gender is required.",
        field: "Gender",
        message: "Gender is required.");

    /// <summary>
    /// Indicates that the nationality is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<PersonalInfo> EmptyNationality() => Result<PersonalInfo>.Invalid(
        detail: "Nationality is required.",
        field: "Nationality",
        message: "Nationality is required.");

    /// <summary>
    /// Indicates that the profession is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<PersonalInfo> EmptyProfession() => Result<PersonalInfo>.Invalid(
        detail: "Profession is required.",
        field: "Profession",
        message: "Profession is required.");

    /// <summary>
    /// Indicates that the birth date is in the future.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<PersonalInfo> FutureBirthDate() => Result<PersonalInfo>.Invalid(
        detail: "Birth date cannot be in the future.",
        field: "BirthDate",
        message: "Birth date cannot be in the future.");

    /// <summary>
    /// Indicates that the first name exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<PersonalInfo> FirstNameTooLong() => Result<PersonalInfo>.Invalid(
        detail: "First name cannot exceed 128 characters.",
        field: "FirstName",
        message: "First name cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the last name exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<PersonalInfo> LastNameTooLong() => Result<PersonalInfo>.Invalid(
        detail: "Last name cannot exceed 128 characters.",
        field: "LastName",
        message: "Last name cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the gender exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<PersonalInfo> GenderTooLong() => Result<PersonalInfo>.Invalid(
        detail: "Gender cannot exceed 64 characters.",
        field: "Gender",
        message: "Gender cannot exceed 64 characters.");

    /// <summary>
    /// Indicates that the nationality exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<PersonalInfo> NationalityTooLong() => Result<PersonalInfo>.Invalid(
        detail: "Nationality cannot exceed 128 characters.",
        field: "Nationality",
        message: "Nationality cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the profession exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<PersonalInfo> ProfessionTooLong() => Result<PersonalInfo>.Invalid(
        detail: "Profession cannot exceed 128 characters.",
        field: "Profession",
        message: "Profession cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the email is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<ContactInfo> EmptyEmail() => Result<ContactInfo>.Invalid(
        detail: "Email is required.",
        field: "Email",
        message: "Email is required.");

    /// <summary>
    /// Indicates that the email exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<ContactInfo> EmailTooLong() => Result<ContactInfo>.Invalid(
        detail: "Email cannot exceed 128 characters.",
        field: "Email",
        message: "Email cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the mobile is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<ContactInfo> EmptyMobile() => Result<ContactInfo>.Invalid(
        detail: "Mobile is required.",
        field: "Mobile",
        message: "Mobile is required.");

    /// <summary>
    /// Indicates that the mobile exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<ContactInfo> MobileTooLong() => Result<ContactInfo>.Invalid(
        detail: "Mobile cannot exceed 64 characters.",
        field: "Mobile",
        message: "Mobile cannot exceed 64 characters.");

    /// <summary>
    /// Indicates that the Instagram handle exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<ContactInfo> InstagramTooLong() => Result<ContactInfo>.Invalid(
        detail: "Instagram cannot exceed 64 characters.",
        field: "Instagram",
        message: "Instagram cannot exceed 64 characters.");

    /// <summary>
    /// Indicates that the Facebook profile exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<ContactInfo> FacebookTooLong() => Result<ContactInfo>.Invalid(
        detail: "Facebook cannot exceed 64 characters.",
        field: "Facebook",
        message: "Facebook cannot exceed 64 characters.");

    /// <summary>
    /// Indicates that the national ID is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<IdentificationInfo> EmptyNationalId() => Result<IdentificationInfo>.Invalid(
        detail: "National ID is required.",
        field: "NationalId",
        message: "National ID is required.");

    /// <summary>
    /// Indicates that the national ID exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<IdentificationInfo> NationalIdTooLong() => Result<IdentificationInfo>.Invalid(
        detail: "National ID cannot exceed 64 characters.",
        field: "NationalId",
        message: "National ID cannot exceed 64 characters.");

    /// <summary>
    /// Indicates that the ID nationality is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<IdentificationInfo> EmptyIdNationality() => Result<IdentificationInfo>.Invalid(
        detail: "ID nationality is required.",
        field: "IdNationality",
        message: "ID nationality is required.");

    /// <summary>
    /// Indicates that the ID nationality exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<IdentificationInfo> IdNationalityTooLong() => Result<IdentificationInfo>.Invalid(
        detail: "ID nationality cannot exceed 64 characters.",
        field: "IdNationality",
        message: "ID nationality cannot exceed 64 characters.");

    /// <summary>
    /// Indicates that the street is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<Address> EmptyStreet() => Result<Address>.Invalid(
        detail: "Street is required.",
        field: "Street",
        message: "Street is required.");

    /// <summary>
    /// Indicates that the street exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<Address> StreetTooLong() => Result<Address>.Invalid(
        detail: "Street cannot exceed 128 characters.",
        field: "Street",
        message: "Street cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the complement exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<Address> ComplementTooLong() => Result<Address>.Invalid(
        detail: "Complement cannot exceed 128 characters.",
        field: "Complement",
        message: "Complement cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the neighborhood is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<Address> EmptyNeighborhood() => Result<Address>.Invalid(
        detail: "Neighborhood is required.",
        field: "Neighborhood",
        message: "Neighborhood is required.");

    /// <summary>
    /// Indicates that the neighborhood exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<Address> NeighborhoodTooLong() => Result<Address>.Invalid(
        detail: "Neighborhood cannot exceed 128 characters.",
        field: "Neighborhood",
        message: "Neighborhood cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the postal code is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<Address> EmptyPostalCode() => Result<Address>.Invalid(
        detail: "Postal code is required.",
        field: "PostalCode",
        message: "Postal code is required.");

    /// <summary>
    /// Indicates that the postal code exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<Address> PostalCodeTooLong() => Result<Address>.Invalid(
        detail: "Postal code cannot exceed 64 characters.",
        field: "PostalCode",
        message: "Postal code cannot exceed 64 characters.");

    /// <summary>
    /// Indicates that the city is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<Address> EmptyCity() => Result<Address>.Invalid(
        detail: "City is required.",
        field: "City",
        message: "City is required.");

    /// <summary>
    /// Indicates that the city exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<Address> CityTooLong() => Result<Address>.Invalid(
        detail: "City cannot exceed 128 characters.",
        field: "City",
        message: "City cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the state is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<Address> EmptyState() => Result<Address>.Invalid(
        detail: "State is required.",
        field: "State",
        message: "State is required.");

    /// <summary>
    /// Indicates that the state exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<Address> StateTooLong() => Result<Address>.Invalid(
        detail: "State cannot exceed 128 characters.",
        field: "State",
        message: "State cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the country is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<Address> EmptyCountry() => Result<Address>.Invalid(
        detail: "Country is required.",
        field: "Country",
        message: "Country is required.");

    /// <summary>
    /// Indicates that the country exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<Address> CountryTooLong() => Result<Address>.Invalid(
        detail: "Country cannot exceed 128 characters.",
        field: "Country",
        message: "Country cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the emergency contact name is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<EmergencyContact> EmptyEmergencyContactName() => Result<EmergencyContact>.Invalid(
        detail: "Emergency contact name is required.",
        field: "Name",
        message: "Emergency contact name is required.");

    /// <summary>
    /// Indicates that the emergency contact name exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<EmergencyContact> EmergencyContactNameTooLong() => Result<EmergencyContact>.Invalid(
        detail: "Emergency contact name cannot exceed 128 characters.",
        field: "Name",
        message: "Emergency contact name cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the emergency contact mobile is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<EmergencyContact> EmptyEmergencyContactMobile() => Result<EmergencyContact>.Invalid(
        detail: "Emergency contact mobile is required.",
        field: "Mobile",
        message: "Emergency contact mobile is required.");

    /// <summary>
    /// Indicates that the emergency contact mobile exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<EmergencyContact> EmergencyContactMobileTooLong() => Result<EmergencyContact>.Invalid(
        detail: "Emergency contact mobile cannot exceed 64 characters.",
        field: "Mobile",
        message: "Emergency contact mobile cannot exceed 64 characters.");

    /// <summary>
    /// Indicates that the weight is invalid (must be between 1 and 500 kg).
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<PhysicalInfo> InvalidWeight() => Result<PhysicalInfo>.Invalid(
        detail: "Weight must be between 1 and 500 kilograms.",
        field: "WeightKg",
        message: "Weight must be between 1 and 500 kilograms.");

    /// <summary>
    /// Indicates that the height is invalid (must be between 50 and 300 cm).
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<PhysicalInfo> InvalidHeight() => Result<PhysicalInfo>.Invalid(
        detail: "Height must be between 50 and 300 centimeters.",
        field: "HeightCentimeters",
        message: "Height must be between 50 and 300 centimeters.");

    /// <summary>
    /// Indicates that allergies text exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<MedicalInfo> AllergiesTooLong() => Result<MedicalInfo>.Invalid(
        detail: "Allergies cannot exceed 500 characters.",
        field: "Allergies",
        message: "Allergies cannot exceed 500 characters.");

    /// <summary>
    /// Indicates that additional medical info exceeds the maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<MedicalInfo> AdditionalInfoTooLong() => Result<MedicalInfo>.Invalid(
        detail: "Additional information cannot exceed 500 characters.",
        field: "AdditionalInfo",
        message: "Additional information cannot exceed 500 characters.");

    /// <summary>
    /// Indicates that a customer with the specified ID was not found.
    /// </summary>
    /// <param name="id">The ID of the customer that was not found.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result CustomerNotFound(Guid id) => Result.NotFound(detail: $"Customer with ID {id} was not found.");
}
