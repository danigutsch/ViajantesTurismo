using JetBrains.Annotations;
using ViajantesTurismo.AdminApi.Contracts;
using ViajantesTurismo.Common;
using ViajantesTurismo.Common.Results;
using static ViajantesTurismo.Admin.Domain.Customers.CustomerErrors;

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
    private Address(string street, string? complement, string neighborhood, string postalCode, string city, string state, string country)
    {
        Street = street;
        Complement = complement;
        Neighborhood = neighborhood;
        PostalCode = postalCode;
        City = city;
        State = state;
        Country = country;
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
    /// Creates a new instance of <see cref="Address"/> with validation.
    /// </summary>
    /// <param name="street">The street address and number.</param>
    /// <param name="complement">The address complement.</param>
    /// <param name="neighborhood">The neighborhood.</param>
    /// <param name="postalCode">The postal code.</param>
    /// <param name="city">The city.</param>
    /// <param name="state">The state.</param>
    /// <param name="country">The country.</param>
    /// <returns>A <see cref="Result{Address}"/> containing the address or validation errors.</returns>
    public static Result<Address> Create(string? street, string? complement, string? neighborhood, string? postalCode, string? city, string? state, string? country)
    {
        var sanitizedStreet = StringSanitizer.Sanitize(street);
        var sanitizedComplement = StringSanitizer.Sanitize(complement);
        sanitizedComplement = string.IsNullOrWhiteSpace(sanitizedComplement) ? null : sanitizedComplement;
        var sanitizedNeighborhood = StringSanitizer.Sanitize(neighborhood);
        var sanitizedPostalCode = StringSanitizer.Sanitize(postalCode);
        var sanitizedCity = StringSanitizer.Sanitize(city);
        var sanitizedState = StringSanitizer.Sanitize(state);
        var sanitizedCountry = StringSanitizer.Sanitize(country);

        var errors = new ValidationErrors();

        if (string.IsNullOrWhiteSpace(sanitizedStreet))
        {
            errors.Add(EmptyStreet());
        }
        else if (sanitizedStreet.Length > ContractConstants.MaxNameLength)
        {
            errors.Add(StreetTooLong());
        }

        if (sanitizedComplement?.Length > ContractConstants.MaxNameLength)
        {
            errors.Add(ComplementTooLong());
        }

        if (string.IsNullOrWhiteSpace(sanitizedNeighborhood))
        {
            errors.Add(EmptyNeighborhood());
        }
        else if (sanitizedNeighborhood.Length > ContractConstants.MaxNameLength)
        {
            errors.Add(NeighborhoodTooLong());
        }

        if (string.IsNullOrWhiteSpace(sanitizedPostalCode))
        {
            errors.Add(EmptyPostalCode());
        }
        else if (sanitizedPostalCode.Length > ContractConstants.MaxDefaultLength)
        {
            errors.Add(PostalCodeTooLong());
        }

        if (string.IsNullOrWhiteSpace(sanitizedCity))
        {
            errors.Add(EmptyCity());
        }
        else if (sanitizedCity.Length > ContractConstants.MaxNameLength)
        {
            errors.Add(CityTooLong());
        }

        if (string.IsNullOrWhiteSpace(sanitizedState))
        {
            errors.Add(EmptyState());
        }
        else if (sanitizedState.Length > ContractConstants.MaxNameLength)
        {
            errors.Add(StateTooLong());
        }

        if (string.IsNullOrWhiteSpace(sanitizedCountry))
        {
            errors.Add(EmptyCountry());
        }
        else if (sanitizedCountry.Length > ContractConstants.MaxNameLength)
        {
            errors.Add(CountryTooLong());
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<Address>();
        }

        return new Address(sanitizedStreet!, sanitizedComplement, sanitizedNeighborhood!, sanitizedPostalCode!, sanitizedCity!, sanitizedState!, sanitizedCountry!);
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
#pragma warning disable CS8618
    [UsedImplicitly]
    private Address()
    {
    }
}