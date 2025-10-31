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
}
