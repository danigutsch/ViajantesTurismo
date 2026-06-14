namespace SharedKernel.Functional.Tests;

/// <summary>
/// Shared trait constants for functional SharedKernel tests.
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
    /// Trait name for test category.
    /// </summary>
    public const string CategoryName = SharedKernel.Testing.SharedKernelTestTraitNames.CategoryName;

    /// <summary>
    /// Trait name for test theory classification.
    /// </summary>
    public const string TheoryName = SharedKernel.Testing.SharedKernelTestTraitNames.TheoryName;

    /// <summary>
    /// Scope value for unit tests.
    /// </summary>
    public const string UnitScope = SharedKernel.Testing.SharedKernelTestTraitNames.UnitScope;

    /// <summary>
    /// Component value for SharedKernel functional tests.
    /// </summary>
    public const string ResultsComponent = "SharedKernel.Functional";

    /// <summary>
    /// Capability value for option tests.
    /// </summary>
    public const string OptionCapability = "Option";

    /// <summary>
    /// Capability value for result tests.
    /// </summary>
    public const string ResultCapability = "Result";

    /// <summary>
    /// Category value for core behavior tests.
    /// </summary>
    public const string CoreBehaviorCategory = "CoreBehavior";

    /// <summary>
    /// Category value for edge case tests.
    /// </summary>
    public const string EdgeCaseCategory = "EdgeCase";

    /// <summary>
    /// Category value for composition tests.
    /// </summary>
    public const string CompositionCategory = "Composition";

    /// <summary>
    /// Theory value for functor law tests.
    /// </summary>
    public const string FunctorLawsTheory = "FunctorLaws";

    /// <summary>
    /// Theory value for monad law tests.
    /// </summary>
    public const string MonadLawsTheory = "MonadLaws";

    /// <summary>
    /// Theory value for match semantics tests.
    /// </summary>
    public const string MatchSemanticsTheory = "MatchSemantics";
}
