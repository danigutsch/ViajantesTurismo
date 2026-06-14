namespace ViajantesTurismo.ArchitectureTests;

/// <summary>
/// Shared trait constants for architecture tests.
/// </summary>
public static class TestTraits
{
    /// <summary>
    /// Trait name for scope.
    /// </summary>
    public const string ScopeName = SharedKernel.Testing.AdminTestTraitNames.ScopeName;

    /// <summary>
    /// Trait name for surface.
    /// </summary>
    public const string SurfaceName = SharedKernel.Testing.AdminTestTraitNames.SurfaceName;

    /// <summary>
    /// Trait name for area.
    /// </summary>
    public const string AreaName = SharedKernel.Testing.AdminTestTraitNames.AreaName;

    /// <summary>
    /// Scope value for architecture tests.
    /// </summary>
    public const string ArchitectureScope = "architecture";

    /// <summary>
    /// Surface value for solution-wide architecture tests.
    /// </summary>
    public const string SolutionSurface = "solution";

    /// <summary>
    /// Area value for shared architecture coverage.
    /// </summary>
    public const string SharedArea = "shared";
}
