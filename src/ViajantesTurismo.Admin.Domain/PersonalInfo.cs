namespace ViajantesTurismo.Admin.Domain;

/// <summary>
/// Represents personal information of a customer.
/// </summary>
public sealed class PersonalInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PersonalInfo"/> class.
    /// </summary>
    /// <param name="firstName">The first name.</param>
    /// <param name="lastName">The last name.</param>
    /// <param name="gender">The gender.</param>
    /// <param name="birthDate">The birth date.</param>
    /// <param name="nationality">The nationality.</param>
    /// <param name="profession">The profession.</param>
    public PersonalInfo(string firstName, string lastName, string gender, DateTime birthDate, string nationality, string profession)
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
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
#pragma warning disable CS8618
    private PersonalInfo()
    {
    }
}
