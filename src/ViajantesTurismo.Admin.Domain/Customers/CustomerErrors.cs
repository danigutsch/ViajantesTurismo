using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Provides predefined customer-related error results.
/// </summary>
public static class CustomerErrors
{
    private const string Mobile = "Mobile";

    /// <summary>
    /// Indicates that the first name is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result EmptyFirstName() => Result.Invalid(
        detail: "First name is required.",
        field: "FirstName",
        message: "First name is required.");

    /// <summary>
    /// Indicates that the last name is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result EmptyLastName() => Result.Invalid(
        detail: "Last name is required.",
        field: "LastName",
        message: "Last name is required.");

    /// <summary>
    /// Indicates that the gender is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result EmptyGender() => Result.Invalid(
        detail: "Gender is required.",
        field: "Gender",
        message: "Gender is required.");

    /// <summary>
    /// Indicates that the nationality is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result EmptyNationality() => Result.Invalid(
        detail: "Nationality is required.",
        field: "Nationality",
        message: "Nationality is required.");

    /// <summary>
    /// Indicates that the occupation is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result EmptyOccupation() => Result.Invalid(
        detail: "Occupation is required.",
        field: "Occupation",
        message: "Occupation is required.");

    /// <summary>
    /// Indicates that the birth date is in the future.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result FutureBirthDate() => Result.Invalid(
        detail: "Birth date cannot be in the future.",
        field: "BirthDate",
        message: "Birth date cannot be in the future.");

    /// <summary>
    /// Indicates that the first name exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result FirstNameTooLong() => Result.Invalid(
        detail: "First name cannot exceed 128 characters.",
        field: "FirstName",
        message: "First name cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the last name exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result LastNameTooLong() => Result.Invalid(
        detail: "Last name cannot exceed 128 characters.",
        field: "LastName",
        message: "Last name cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the gender exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result GenderTooLong() => Result.Invalid(
        detail: "Gender cannot exceed 64 characters.",
        field: "Gender",
        message: "Gender cannot exceed 64 characters.");

    /// <summary>
    /// Indicates that the nationality exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result NationalityTooLong() => Result.Invalid(
        detail: "Nationality cannot exceed 128 characters.",
        field: "Nationality",
        message: "Nationality cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the occupation exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result OccupationTooLong() => Result.Invalid(
        detail: "Occupation cannot exceed 128 characters.",
        field: "Occupation",
        message: "Occupation cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the email is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result EmptyEmail() => Result.Invalid(
        detail: "Email is required.",
        field: "Email",
        message: "Email is required.");

    /// <summary>
    /// Indicates that the email exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result EmailTooLong() => Result.Invalid(
        detail: "Email cannot exceed 128 characters.",
        field: "Email",
        message: "Email cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the mobile is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result EmptyMobile() => Result.Invalid(
        detail: "Mobile is required.",
        field: Mobile,
        message: "Mobile is required.");

    /// <summary>
    /// Indicates that the mobile exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result MobileTooLong() => Result.Invalid(
        detail: "Mobile cannot exceed 64 characters.",
        field: Mobile,
        message: "Mobile cannot exceed 64 characters.");

    /// <summary>
    /// Indicates that the Instagram handle exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result InstagramTooLong() => Result.Invalid(
        detail: "Instagram cannot exceed 64 characters.",
        field: "Instagram",
        message: "Instagram cannot exceed 64 characters.");

    /// <summary>
    /// Indicates that the Facebook profile exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result FacebookTooLong() => Result.Invalid(
        detail: "Facebook cannot exceed 64 characters.",
        field: "Facebook",
        message: "Facebook cannot exceed 64 characters.");

    /// <summary>
    /// Indicates that the national ID is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result EmptyNationalId() => Result.Invalid(
        detail: "National ID is required.",
        field: "NationalId",
        message: "National ID is required.");

    /// <summary>
    /// Indicates that the national ID exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result NationalIdTooLong() => Result.Invalid(
        detail: "National ID cannot exceed 64 characters.",
        field: "NationalId",
        message: "National ID cannot exceed 64 characters.");

    /// <summary>
    /// Indicates that the ID nationality is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result EmptyIdNationality() => Result.Invalid(
        detail: "ID nationality is required.",
        field: "IdNationality",
        message: "ID nationality is required.");

    /// <summary>
    /// Indicates that the ID nationality exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result IdNationalityTooLong() => Result.Invalid(
        detail: "ID nationality cannot exceed 64 characters.",
        field: "IdNationality",
        message: "ID nationality cannot exceed 64 characters.");

    /// <summary>
    /// Indicates that the street is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result EmptyStreet() => Result.Invalid(
        detail: "Street is required.",
        field: "Street",
        message: "Street is required.");

    /// <summary>
    /// Indicates that the street exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result StreetTooLong() => Result.Invalid(
        detail: "Street cannot exceed 128 characters.",
        field: "Street",
        message: "Street cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the complement exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result ComplementTooLong() => Result.Invalid(
        detail: "Complement cannot exceed 128 characters.",
        field: "Complement",
        message: "Complement cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the neighborhood is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result EmptyNeighborhood() => Result.Invalid(
        detail: "Neighborhood is required.",
        field: "Neighborhood",
        message: "Neighborhood is required.");

    /// <summary>
    /// Indicates that the neighborhood exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result NeighborhoodTooLong() => Result.Invalid(
        detail: "Neighborhood cannot exceed 128 characters.",
        field: "Neighborhood",
        message: "Neighborhood cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the postal code is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result EmptyPostalCode() => Result.Invalid(
        detail: "Postal code is required.",
        field: "PostalCode",
        message: "Postal code is required.");

    /// <summary>
    /// Indicates that the postal code exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result PostalCodeTooLong() => Result.Invalid(
        detail: "Postal code cannot exceed 64 characters.",
        field: "PostalCode",
        message: "Postal code cannot exceed 64 characters.");

    /// <summary>
    /// Indicates that the city is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result EmptyCity() => Result.Invalid(
        detail: "City is required.",
        field: "City",
        message: "City is required.");

    /// <summary>
    /// Indicates that the city exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result CityTooLong() => Result.Invalid(
        detail: "City cannot exceed 128 characters.",
        field: "City",
        message: "City cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the state is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result EmptyState() => Result.Invalid(
        detail: "State is required.",
        field: "State",
        message: "State is required.");

    /// <summary>
    /// Indicates that the state exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result StateTooLong() => Result.Invalid(
        detail: "State cannot exceed 128 characters.",
        field: "State",
        message: "State cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the country is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result EmptyCountry() => Result.Invalid(
        detail: "Country is required.",
        field: "Country",
        message: "Country is required.");

    /// <summary>
    /// Indicates that the country exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result CountryTooLong() => Result.Invalid(
        detail: "Country cannot exceed 128 characters.",
        field: "Country",
        message: "Country cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the emergency contact name is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result EmptyEmergencyContactName() => Result.Invalid(
        detail: "Emergency contact name is required.",
        field: "Name",
        message: "Emergency contact name is required.");

    /// <summary>
    /// Indicates that the emergency contact name exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result EmergencyContactNameTooLong() => Result.Invalid(
        detail: "Emergency contact name cannot exceed 128 characters.",
        field: "Name",
        message: "Emergency contact name cannot exceed 128 characters.");

    /// <summary>
    /// Indicates that the emergency contact mobile is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result EmptyEmergencyContactMobile() => Result.Invalid(
        detail: "Emergency contact mobile is required.",
        field: Mobile,
        message: "Emergency contact mobile is required.");

    /// <summary>
    /// Indicates that the emergency contact mobile exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result EmergencyContactMobileTooLong() => Result.Invalid(
        detail: "Emergency contact mobile cannot exceed 64 characters.",
        field: Mobile,
        message: "Emergency contact mobile cannot exceed 64 characters.");

    /// <summary>
    /// Indicates that the weight is invalid (must be between 1 and 500 kg).
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result InvalidWeight() => Result.Invalid(
        detail: "Weight must be between 1 and 500 kilograms.",
        field: "WeightKg",
        message: "Weight must be between 1 and 500 kilograms.");

    /// <summary>
    /// Indicates that the height is invalid (must be between 50 and 300 cm).
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result InvalidHeight() => Result.Invalid(
        detail: "Height must be between 50 and 300 centimeters.",
        field: "HeightCentimeters",
        message: "Height must be between 50 and 300 centimeters.");

    /// <summary>
    /// Indicates that allergies text exceeds maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result AllergiesTooLong() => Result.Invalid(
        detail: "Allergies cannot exceed 500 characters.",
        field: "Allergies",
        message: "Allergies cannot exceed 500 characters.");

    /// <summary>
    /// Indicates that additional medical info exceeds the maximum length.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result AdditionalInfoTooLong() => Result.Invalid(
        detail: "Additional information cannot exceed 500 characters.",
        field: "AdditionalInfo",
        message: "Additional information cannot exceed 500 characters.");

    /// <summary>
    /// Indicates that a customer with the specified ID was not found.
    /// </summary>
    /// <param name="id">The ID of the customer that was not found.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result CustomerNotFound(Guid id) => Result.NotFound(detail: $"Customer with ID {id} was not found.");

    /// <summary>
    /// Indicates that the email format is invalid.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result InvalidEmailFormat() => Result.Invalid(
        detail: "Email must be in a valid format (e.g., user@example.com).",
        field: "Email",
        message: "Email must be in a valid format (e.g., user@example.com).");

    /// <summary>
    /// Indicates that the mobile phone format is invalid.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result InvalidPhoneFormat() => Result.Invalid(
        detail: "Mobile phone must contain only digits, spaces, hyphens, parentheses, or plus sign.",
        field: Mobile,
        message: "Mobile phone must contain only digits, spaces, hyphens, parentheses, or plus sign.");

    /// <summary>
    /// Indicates that the customer is too young (minimum age is 10 years).
    /// </summary>
    /// <param name="age">The calculated age of the customer.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result AgeTooYoung(int age) => Result.Invalid(
        detail: $"Customer must be at least 10 years old. Current age: {age}.",
        field: "BirthDate",
        message: $"Customer must be at least 10 years old. Current age: {age}.");

    /// <summary>
    /// Indicates that a customer with the specified email already exists.
    /// </summary>
    /// <param name="email">The email address that already exists.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result EmailAlreadyExists(string email) => Result.Conflict(
        detail: $"A customer with email '{email}' already exists.");
}
