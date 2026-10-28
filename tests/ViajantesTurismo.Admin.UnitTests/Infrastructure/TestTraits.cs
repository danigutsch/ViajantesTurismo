namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>
/// Trait names and values used to categorize unit tests.
/// </summary>
internal static class TestTraits
{
    /// <summary>
    /// Trait value for outbox tests.
    /// </summary>
    public const string OutboxCategory = "outbox";

    /// <summary>
    /// Trait value for persistence tests.
    /// </summary>
    public const string PersistenceCategory = "persistence";

    /// <summary>
    /// Trait value for security tests.
    /// </summary>
    public const string SecurityCategory = PandaBear.Testing.TestTraitValues.SecurityCategory;

    /// <summary>
    /// Trait value for the integration event relay capability.
    /// </summary>
    public const string IntegrationEventRelayCapability = "integration-event-relay";

    /// <summary>
    /// Trait value for core behavior tests.
    /// </summary>
    public const string CoreBehaviorCategory = "core-behavior";

    /// <summary>
    /// Trait value for the payments capability.
    /// </summary>
    public const string PaymentsCapability = "Payments";

}
