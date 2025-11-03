namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// Data Transfer Object representing a customer summary for listing and display purposes.
/// Contains essential customer information without full details.
/// </summary>
public sealed record GetCustomerDto
{
    /// <summary>
    /// The unique identifier of the customer.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// The first name of the customer.
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    /// The last name of the customer.
    /// </summary>
    public required string LastName { get; init; }

    /// <summary>
    /// The email address of the customer.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// The mobile phone number of the customer.
    /// </summary>
    public required string Mobile { get; init; }

    /// <summary>
    /// The nationality of the customer.
    /// </summary>
    public required string Nationality { get; init; }

    /// <summary>
    /// The preferred bike type from customer's physical information.
    /// </summary>
    public BikeTypeDto? BikeType { get; init; }
}