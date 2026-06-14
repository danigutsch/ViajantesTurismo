namespace SharedKernel.Mediator.GeneratorTests;

/// <summary>
/// Shared trait constants for mediator source generator tests.
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
    /// Component value for source generator tests.
    /// </summary>
    public const string SourceGeneratorComponent = "SharedKernel.Mediator.SourceGenerator";

    /// <summary>
    /// Capability value for discovery tests.
    /// </summary>
    public const string DiscoveryCapability = "Discovery";

    /// <summary>
    /// Capability value for dependency injection tests.
    /// </summary>
    public const string DependencyInjectionCapability = "DependencyInjection";

    /// <summary>
    /// Capability value for dispatch tests.
    /// </summary>
    public const string DispatchCapability = "Dispatch";
}
