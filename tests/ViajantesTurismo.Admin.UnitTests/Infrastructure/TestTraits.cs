namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal static class TestTraits
{
    public const string OutboxCategory = "outbox";

    public const string PersistenceCategory = "persistence";

    public const string SecurityCategory = SharedKernel.Testing.TestTraitValues.SecurityCategory;

    public const string IntegrationEventRelayCapability = "integration-event-relay";

    public const string CoreBehaviorCategory = "core-behavior";

}
