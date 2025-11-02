using JetBrains.Annotations;
using ViajantesTurismo.Common;

namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Represents a physical address.
/// </summary>
public sealed class Address
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Address"/> class.
    /// </summary>
    /// <param name="street">The street address and number.</param>
    /// <param name="complement">The address complement.</param>
    /// <param name="neighborhood">The neighborhood.</param>
    /// <param name="postalCode">The postal code.</param>
    /// <param name="city">The city.</param>
    /// <param name="state">The state.</param>
    /// <param name="country">The country.</param>
    public Address(string street, string? complement, string neighborhood, string postalCode, string city, string state, string country)
    {
        Street = StringSanitizer.Sanitize(street);
        Complement = StringSanitizer.Sanitize(complement);
        Neighborhood = StringSanitizer.Sanitize(neighborhood);
        PostalCode = StringSanitizer.Sanitize(postalCode);
        City = StringSanitizer.Sanitize(city);
        State = StringSanitizer.Sanitize(state);
        Country = StringSanitizer.Sanitize(country);
    }

    /// <summary>Street address and number.</summary>
    public string Street { get; private set; }

    /// <summary>Address complement.</summary>
    public string? Complement { get; private set; }

    /// <summary>Neighborhood.</summary>
    public string Neighborhood { get; private set; }

    /// <summary>Postal code.</summary>
    public string PostalCode { get; private set; }

    /// <summary>City.</summary>
    public string City { get; private set; }

    /// <summary>State.</summary>
    public string State { get; private set; }

    /// <summary>Country.</summary>
    public string Country { get; private set; }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
#pragma warning disable CS8618
    [UsedImplicitly]
    private Address()
    {
    }
}
