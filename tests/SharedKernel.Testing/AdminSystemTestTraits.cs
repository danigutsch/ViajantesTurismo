namespace ViajantesTurismo.Admin.SystemTests.Infrastructure;

/// <summary>
/// Shared trait constants for Admin system tests.
/// </summary>
public static class TestTraits
{
    /// <summary>
    /// Trait name for category.
    /// </summary>
    public const string CategoryName = SharedKernel.Testing.AdminTestTraitNames.CategoryName;

    /// <summary>
    /// Trait name for scope.
    /// </summary>
    public const string ScopeName = SharedKernel.Testing.AdminTestTraitNames.ScopeName;

    /// <summary>
    /// Trait name for area.
    /// </summary>
    public const string AreaName = SharedKernel.Testing.AdminTestTraitNames.AreaName;

    /// <summary>
    /// Trait name for host.
    /// </summary>
    public const string HostName = SharedKernel.Testing.AdminTestTraitNames.HostName;

    /// <summary>
    /// Category value for migration-focused tests.
    /// </summary>
    public const string MigrationCategory = "migration";

    /// <summary>
    /// Scope value for system tests.
    /// </summary>
    public const string SystemScope = "system";

    /// <summary>
    /// Area value for shared system test coverage.
    /// </summary>
    public const string SharedArea = "shared";

    /// <summary>
    /// Host value for Aspire-hosted tests.
    /// </summary>
    public const string AspireHost = "aspire";
}
