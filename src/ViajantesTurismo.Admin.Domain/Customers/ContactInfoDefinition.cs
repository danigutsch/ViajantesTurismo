namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Defines the raw contact values of a customer.
/// </summary>
/// <param name="Email">The email address.</param>
/// <param name="Mobile">The mobile phone number.</param>
/// <param name="Instagram">The optional Instagram handle.</param>
/// <param name="Facebook">The optional Facebook profile.</param>
public sealed record ContactInfoDefinition(string Email, string Mobile, string? Instagram, string? Facebook);
