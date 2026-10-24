using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Defines the raw physical characteristics and bike preference of a customer.
/// </summary>
/// <param name="WeightKg">The weight in kilograms.</param>
/// <param name="HeightCentimeters">The height in centimeters.</param>
/// <param name="BikeType">The preferred bike type.</param>
public sealed record PhysicalInfoDefinition(decimal WeightKg, int HeightCentimeters, BikeType BikeType);
