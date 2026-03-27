namespace ViajantesTurismo.Admin.Tests.Shared.Behavior;

/// <summary>
/// Options for creating a tour in tests.
/// </summary>
/// <param name="Identifier">The tour identifier override.</param>
/// <param name="Name">The tour name override.</param>
/// <param name="Schedule">The schedule overrides.</param>
/// <param name="Pricing">The pricing overrides.</param>
/// <param name="Capacity">The capacity overrides.</param>
/// <param name="IncludedServices">The included services override.</param>
public sealed record TourOptions(
    string? Identifier = null,
    string? Name = null,
    TourScheduleOptions? Schedule = null,
    TourPricingOptions? Pricing = null,
    TourCapacityOptions? Capacity = null,
    IReadOnlyList<string>? IncludedServices = null);
