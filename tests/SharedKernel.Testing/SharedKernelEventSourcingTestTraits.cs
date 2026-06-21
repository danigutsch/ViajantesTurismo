namespace SharedKernel.EventSourcing.Tests;

/// <summary>
/// Shared trait constants for SharedKernel event-sourcing tests.
/// </summary>
public static class TestTraits
{
    /// <summary>
    /// Trait name for test scope.
    /// </summary>
    public const string ScopeName = SharedKernel.Testing.SharedKernelTestTraitNames.ScopeName;

    /// <summary>
    /// Trait name for component ownership.
    /// </summary>
    public const string ComponentName = SharedKernel.Testing.SharedKernelTestTraitNames.ComponentName;

    /// <summary>
    /// Unit test scope value.
    /// </summary>
    public const string UnitScope = SharedKernel.Testing.SharedKernelTestTraitNames.UnitScope;

    /// <summary>
    /// Component value for SharedKernel event-sourcing tests.
    /// </summary>
    public const string EventSourcingComponent = "SharedKernel.EventSourcing";
}
