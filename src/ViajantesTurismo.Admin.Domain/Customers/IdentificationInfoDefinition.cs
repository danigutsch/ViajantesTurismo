namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Defines the raw identification values of a customer.
/// </summary>
/// <param name="NationalId">The national identification number.</param>
/// <param name="IdNationality">The nationality that issued the identification.</param>
public sealed record IdentificationInfoDefinition(string NationalId, string IdNationality);
