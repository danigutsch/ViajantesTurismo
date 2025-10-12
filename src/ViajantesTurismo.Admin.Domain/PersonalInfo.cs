namespace ViajantesTurismo.Admin.Domain;

/// <summary>
/// Represents personal information of a customer.
/// </summary>
public sealed class PersonalInfo
{
    /// <summary>Full first name of the customer.</summary>
    public required string FirstName { get; init; }

    /// <summary>Full last name of the customer.</summary>
    public required string LastName { get; init; }

    /// <summary>Gender of the customer.</summary>
    public required string Gender { get; init; }

    /// <summary>Date of birth.</summary>
    public required DateTime BirthDate { get; init; }

    /// <summary>Nationality.</summary>
    public required string Nationality { get; init; }

    /// <summary>Profession.</summary>
    public required string Profession { get; init; }
}