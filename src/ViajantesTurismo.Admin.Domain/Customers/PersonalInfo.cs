using JetBrains.Annotations;
using ViajantesTurismo.AdminApi.Contracts;
using ViajantesTurismo.Common.Results;
using static ViajantesTurismo.Admin.Domain.Customers.CustomerErrors;

namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Represents personal information of a customer.
/// </summary>
public sealed class PersonalInfo
{
    private PersonalInfo(string firstName, string lastName, string gender, DateTime birthDate, string nationality, string profession)
    {
        FirstName = firstName;
        LastName = lastName;
        Gender = gender;
        BirthDate = birthDate;
        Nationality = nationality;
        Profession = profession;
    }

    /// <summary>Full first name of the customer.</summary>
    public string FirstName { get; private set; }

    /// <summary>Full last name of the customer.</summary>
    public string LastName { get; private set; }

    /// <summary>Gender of the customer.</summary>
    public string Gender { get; private set; }

    /// <summary>Date of birth.</summary>
    public DateTime BirthDate { get; private set; }

    /// <summary>Nationality.</summary>
    public string Nationality { get; private set; }

    /// <summary>Profession.</summary>
    public string Profession { get; private set; }

    /// <summary>
    /// Creates a new instance of the <see cref="PersonalInfo"/> class.
    /// </summary>
    /// <param name="firstName">The first name.</param>
    /// <param name="lastName">The last name.</param>
    /// <param name="gender">The gender.</param>
    /// <param name="birthDate">The birthdate.</param>
    /// <param name="nationality">The nationality.</param>
    /// <param name="profession">The profession.</param>
    /// <param name="timeProvider">The time provider for date validation.</param>
    /// <returns>A Result containing the PersonalInfo.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="timeProvider"/> is null.</exception>
    public static Result<PersonalInfo> Create(
        string firstName,
        string lastName,
        string gender,
        DateTime birthDate,
        string nationality,
        string profession,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (string.IsNullOrWhiteSpace(firstName))
        {
            return EmptyFirstName();
        }

        if (firstName.Length > ContractConstants.MaxNameLength)
        {
            return FirstNameTooLong();
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return EmptyLastName();
        }

        if (lastName.Length > ContractConstants.MaxNameLength)
        {
            return LastNameTooLong();
        }

        if (string.IsNullOrWhiteSpace(gender))
        {
            return EmptyGender();
        }

        if (gender.Length > ContractConstants.MaxDefaultLength)
        {
            return GenderTooLong();
        }

        if (string.IsNullOrWhiteSpace(nationality))
        {
            return EmptyNationality();
        }

        if (nationality.Length > ContractConstants.MaxNameLength)
        {
            return NationalityTooLong();
        }

        if (string.IsNullOrWhiteSpace(profession))
        {
            return EmptyProfession();
        }

        if (profession.Length > ContractConstants.MaxNameLength)
        {
            return ProfessionTooLong();
        }

        if (birthDate > timeProvider.GetUtcNow().Date)
        {
            return FutureBirthDate();
        }

        return new PersonalInfo(firstName, lastName, gender, birthDate, nationality, profession);
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
#pragma warning disable CS8618
    [UsedImplicitly]
    private PersonalInfo()
    {
    }
}
