using PandaBear.BuildingBlocks;
using SharedBirthDate = PandaBear.BuildingBlocks.BirthDate;
using PandaBear.Results;
using static ViajantesTurismo.Admin.Domain.Customers.CustomerErrors;
using PandaBear.InputNormalization;

namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Represents personal information of a customer.
/// </summary>
public sealed class PersonalInfo
{
    /// <summary>Customers have no maximum age; <see cref="SharedBirthDate"/> requires one, so the largest value is passed.</summary>
    private const int NoMaximumAgeYears = int.MaxValue;

    private PersonalInfo(string firstName, string lastName, string gender, SharedBirthDate birthDate, string nationality, string occupation)
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
    public SharedBirthDate BirthDate
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

        var name = PersonName.Create(firstName, lastName);
        if (name.IsFailure)
        {
            AddNameErrors(errors, name.ErrorDetails.ValidationErrors);
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

        var validBirthDate = SharedBirthDate.Create(birthDate, today, AdminDomainLimits.MinCustomerAgeYears, NoMaximumAgeYears);
        if (validBirthDate.IsFailure)
        {
            errors.Add(BirthDateError(birthDate, today));
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<PersonalInfo>();
        }

        return Result.Ok(new PersonalInfo(name.Value.FirstName, name.Value.LastName, gender, validBirthDate.Value, nationality, occupation));
    }

    /// <summary>
    /// Reports <see cref="PersonName"/> failures under the customer field names. The shared type uses the same messages, so
    /// only the field name changes.
    /// </summary>
    private static void AddNameErrors(ValidationErrors errors, IReadOnlyDictionary<string, IReadOnlyList<string>>? nameErrors)
    {
        foreach (var (field, messages) in nameErrors ?? new Dictionary<string, IReadOnlyList<string>>())
        {
            var customerField = field switch
            {
                "firstName" => "FirstName",
                "lastName" => "LastName",
                _ => field
            };

            foreach (var message in messages)
            {
                errors.Add(Result.Invalid(detail: message, field: customerField, message: message));
            }
        }
    }

    /// <summary>
    /// Names the rule that <see cref="SharedBirthDate"/> rejected, in its order of checks, without echoing the age: a missing date,
    /// a future date, then a customer younger than the minimum age.
    /// </summary>
    private static Result BirthDateError(DateOnly birthDate, DateOnly today)
    {
        if (birthDate == default)
        {
            return EmptyBirthDate();
        }

        return birthDate > today ? FutureBirthDate() : AgeTooYoung();
    }
}
