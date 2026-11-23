namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Represents one row in the customer overview.</summary>
/// <param name="Id">The identifier used to view or edit the customer.</param>
/// <param name="FirstName">The customer's first name.</param>
/// <param name="LastName">The customer's last name.</param>
/// <param name="Email">The customer's email address.</param>
/// <param name="Mobile">The customer's mobile number.</param>
/// <param name="Nationality">The customer's nationality.</param>
public sealed record GetCustomerListItemDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Mobile,
    string Nationality);
