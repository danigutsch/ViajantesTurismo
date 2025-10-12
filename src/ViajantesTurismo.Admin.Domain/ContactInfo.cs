namespace ViajantesTurismo.Admin.Domain;

/// <summary>
/// Represents contact information of a customer.
/// </summary>
public sealed class ContactInfo
{
    /// <summary>Email address.</summary>
    public required string Email { get; init; }

    /// <summary>Mobile phone number.</summary>
    public required string Mobile { get; init; }

    /// <summary>Instagram handle.</summary>
    public string? Instagram { get; init; }

    /// <summary>Facebook profile.</summary>
    public string? Facebook { get; init; }
}