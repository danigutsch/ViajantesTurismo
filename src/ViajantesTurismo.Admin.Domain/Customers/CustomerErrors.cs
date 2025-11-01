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
}
