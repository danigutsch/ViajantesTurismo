namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Defines the capacity requested for a tour.
/// </summary>
/// <param name="MinCustomers">The minimum customer count.</param>
/// <param name="MaxCustomers">The maximum customer count.</param>
public sealed record TourCapacityDefinition(int MinCustomers, int MaxCustomers);
