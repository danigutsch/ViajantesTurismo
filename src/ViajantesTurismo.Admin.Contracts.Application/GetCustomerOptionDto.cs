namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>A customer choice displayed in an Admin form.</summary>
/// <param name="Id">The customer identifier.</param>
/// <param name="FirstName">The customer first name.</param>
/// <param name="LastName">The customer last name.</param>
/// <param name="Email">The email address used to distinguish similarly named customers.</param>
/// <param name="BikeType">The customer's preferred bike for a booking.</param>
public sealed record GetCustomerOptionDto(Guid Id, string FirstName, string LastName, string Email, BikeTypeDto BikeType);
