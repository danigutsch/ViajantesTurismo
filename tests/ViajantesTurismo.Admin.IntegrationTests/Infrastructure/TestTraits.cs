namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

/// <summary>
/// Shared trait constants for Admin integration tests.
/// </summary>
public static class TestTraits
{
    /// <summary>
    /// Trait name for category.
    /// </summary>
    public const string CategoryName = SharedKernel.Testing.TestTraitNames.CategoryName;

    /// <summary>
    /// Trait name for scope.
    /// </summary>
    public const string ScopeName = SharedKernel.Testing.TestTraitNames.ScopeName;

    /// <summary>
    /// Trait name for area.
    /// </summary>
    public const string AreaName = SharedKernel.Testing.TestTraitNames.AreaName;

    /// <summary>
    /// Category value for smoke coverage.
    /// </summary>
    public const string SmokeCategory = "smoke";

    /// <summary>
    /// Scope value for integration tests.
    /// </summary>
    public const string IntegrationScope = "integration";

    /// <summary>
    /// Area value for bookings tests.
    /// </summary>
    public const string BookingsArea = "bookings";
}
