namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Defines the raw personal information values of a customer.
/// </summary>
/// <param name="FirstName">The first name.</param>
/// <param name="LastName">The last name.</param>
/// <param name="Gender">The gender.</param>
/// <param name="BirthDate">The birth date.</param>
/// <param name="Nationality">The nationality.</param>
/// <param name="Occupation">The occupation.</param>
public sealed record PersonalInfoDefinition(
    string FirstName,
    string LastName,
    string Gender,
    DateOnly BirthDate,
    string Nationality,
    string Occupation);
