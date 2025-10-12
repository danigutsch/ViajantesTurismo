namespace ViajantesTurismo.Admin.Domain;

/// <summary>
/// Represents a physical address.
/// </summary>
public sealed class Address
{
    /// <summary>Street address and number.</summary>
    public required string Street { get; init; }

    /// <summary>Address complement.</summary>
    public string? Complement { get; init; }

    /// <summary>Neighborhood.</summary>
    public required string Neighborhood { get; init; }

    /// <summary>Postal code.</summary>
    public required string PostalCode { get; init; }

    /// <summary>City.</summary>
    public required string City { get; init; }

    /// <summary>State.</summary>
    public required string State { get; init; }

    /// <summary>Country.</summary>
    public required string Country { get; init; }
}