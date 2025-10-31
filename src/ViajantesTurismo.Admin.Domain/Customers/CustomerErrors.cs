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
        validationErrors: new Dictionary<string, string[]>
        {
            { "FirstName", ["First name is required."] }
        });

    /// <summary>
    /// Indicates that the last name is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<PersonalInfo> EmptyLastName() => Result<PersonalInfo>.Invalid(
        detail: "Last name is required.",
        validationErrors: new Dictionary<string, string[]>
        {
            { "LastName", ["Last name is required."] }
        });

    /// <summary>
    /// Indicates that the gender is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<PersonalInfo> EmptyGender() => Result<PersonalInfo>.Invalid(
        detail: "Gender is required.",
        validationErrors: new Dictionary<string, string[]>
        {
            { "Gender", ["Gender is required."] }
        });

    /// <summary>
    /// Indicates that the nationality is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<PersonalInfo> EmptyNationality() => Result<PersonalInfo>.Invalid(
        detail: "Nationality is required.",
        validationErrors: new Dictionary<string, string[]>
        {
            { "Nationality", ["Nationality is required."] }
        });

    /// <summary>
    /// Indicates that the profession is empty.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<PersonalInfo> EmptyProfession() => Result<PersonalInfo>.Invalid(
        detail: "Profession is required.",
        validationErrors: new Dictionary<string, string[]>
        {
            { "Profession", ["Profession is required."] }
        });

    /// <summary>
    /// Indicates that the birth date is in the future.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<PersonalInfo> FutureBirthDate() => Result<PersonalInfo>.Invalid(
        detail: "Birth date cannot be in the future.",
        validationErrors: new Dictionary<string, string[]>
        {
            { "BirthDate", ["Birth date cannot be in the future."] }
        });
}
