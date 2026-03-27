namespace ViajantesTurismo.Admin.Tests.Shared.Behavior;

/// <summary>
/// Options for overriding tour capacity values in tests.
/// </summary>
/// <param name="MinCustomers">The minimum customer count override.</param>
/// <param name="MaxCustomers">The maximum customer count override.</param>
public sealed record TourCapacityOptions(int? MinCustomers = null, int? MaxCustomers = null);
