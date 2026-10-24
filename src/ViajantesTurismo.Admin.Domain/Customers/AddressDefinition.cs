namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Defines the raw address values of a customer.
/// </summary>
/// <param name="Street">The street.</param>
/// <param name="Complement">The optional complement.</param>
/// <param name="Neighborhood">The neighborhood.</param>
/// <param name="PostalCode">The postal code.</param>
/// <param name="City">The city.</param>
/// <param name="State">The state.</param>
/// <param name="Country">The country.</param>
public sealed record AddressDefinition(
    string? Street,
    string? Complement,
    string? Neighborhood,
    string? PostalCode,
    string? City,
    string? State,
    string? Country);
