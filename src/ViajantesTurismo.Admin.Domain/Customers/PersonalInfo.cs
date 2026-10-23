using PandaBear.Results;
using static ViajantesTurismo.Admin.Domain.Customers.CustomerErrors;
using PandaBear.InputNormalization;

namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Represents personal information of a customer.
/// </summary>
public sealed class PersonalInfo
{
    private PersonalInfo(string firstName, string lastName, string gender, DateOnly birthDate, string nationality, string occupation)
    {
        FirstName = firstName;
        LastName = lastName;
        Gender = gender;
        BirthDate = birthDate;
        Nationality = nationality;
        Occupation = occupation;
    }

    /// <summary>Full first name of the customer.</summary>
    public string FirstName
    {
        get; private set;
    }

    /// <summary>Full last name of the customer.</summary>
    public string LastName
    {
        get; private set;
    }

    /// <summary>Gender of the customer.</summary>
    public string Gender
    {
        get; private set;
    }

    /// <summary>Date of birth.</summary>
    public DateOnly BirthDate
    {
        get; private set;
    }

    /// <summary>Nationality.</summary>
    public string Nationality
    {
        get; private set;
    }

    /// <summary>Occupation.</summary>
    public string Occupation
    {
        get; private set;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="PersonalInfo"/> class.
    /// </summary>
    /// <param name="firstName">The first name.</param>
    /// <param name="lastName">The last name.</param>
    /// <param name="gender">The gender.</param>
    /// <param name="birthDate">The birthdate.</param>
    /// <param name="nationality">The nationality.</param>
    /// <param name="occupation">The occupation.</param>
    /// <param name="today">The captured UTC business date for age validation.</param>
    /// <returns>A Result containing the PersonalInfo.</returns>
    public static Result<PersonalInfo> Create(
        string firstName,
        string lastName,
        string gender,
        DateOnly birthDate,
        string nationality,
        string occupation,
        DateOnly today)
    {
        firstName = StringSanitizer.Sanitize(firstName);
        lastName = StringSanitizer.Sanitize(lastName);
        gender = StringSanitizer.Sanitize(gender);
        nationality = StringSanitizer.Sanitize(nationality);
        occupation = StringSanitizer.Sanitize(occupation);

        var errors = new ValidationErrors();

        if (string.IsNullOrWhiteSpace(firstName))
        {
            errors.Add(EmptyFirstName());
        }
        else if (firstName.Length > AdminDomainLimits.MaxNameLength)
        {
            errors.Add(FirstNameTooLong());
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            errors.Add(EmptyLastName());
        }
        else if (lastName.Length > AdminDomainLimits.MaxNameLength)
        {
            errors.Add(LastNameTooLong());
        }

        if (string.IsNullOrWhiteSpace(gender))
        {
            errors.Add(EmptyGender());
        }
        else if (gender.Length > AdminDomainLimits.MaxDefaultLength)
        {
            errors.Add(GenderTooLong());
        }

        if (string.IsNullOrWhiteSpace(nationality))
        {
            errors.Add(EmptyNationality());
        }
        else if (nationality.Length > AdminDomainLimits.MaxNameLength)
        {
            errors.Add(NationalityTooLong());
        }

        if (string.IsNullOrWhiteSpace(occupation))
        {
            errors.Add(EmptyOccupation());
        }
        else if (occupation.Length > AdminDomainLimits.MaxNameLength)
        {
            errors.Add(OccupationTooLong());
        }

        if (birthDate > today)
        {
            errors.Add(FutureBirthDate());
        }
        else
        {
            var age = CalculateAge(birthDate, today);
            if (age < 10)
            {
                errors.Add(AgeTooYoung(age));
            }
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<PersonalInfo>();
        }

        return Result.Ok(new PersonalInfo(firstName, lastName, gender, birthDate, nationality, occupation));
    }

    private static int CalculateAge(DateOnly birthDate, DateOnly currentDate)
    {
        var age = currentDate.Year - birthDate.Year;
        if (currentDate.Month < birthDate.Month || (currentDate.Month == birthDate.Month && currentDate.Day < birthDate.Day))
        {
            age--;
        }

        return age;
    }
}
