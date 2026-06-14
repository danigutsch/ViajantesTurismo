namespace SharedKernel.Mediator.Tests;

/// <summary>
/// Shared trait constants for mediator abstraction tests.
/// </summary>
public static class TestTraits
{
    /// <summary>
    /// Trait name for test scope.
    /// </summary>
    public const string ScopeName = SharedKernel.Testing.SharedKernelTestTraitNames.ScopeName;

    /// <summary>
    /// Trait name for test component.
    /// </summary>
    public const string ComponentName = SharedKernel.Testing.SharedKernelTestTraitNames.ComponentName;

    /// <summary>
    /// Trait name for test capability.
    /// </summary>
    public const string CapabilityName = SharedKernel.Testing.SharedKernelTestTraitNames.CapabilityName;

    /// <summary>
    /// Scope value for unit tests.
    /// </summary>
    public const string UnitScope = SharedKernel.Testing.SharedKernelTestTraitNames.UnitScope;

    /// <summary>
    /// Component value for mediator abstractions tests.
    /// </summary>
    public const string AbstractionsComponent = "SharedKernel.Mediator.Abstractions";

    /// <summary>
    /// Capability value for contract tests.
    /// </summary>
    public const string ContractsCapability = "Contracts";

    /// <summary>
    /// Capability value for reference dispatcher tests.
    /// </summary>
    public const string ReferenceDispatcherCapability = "ReferenceDispatcher";
}
