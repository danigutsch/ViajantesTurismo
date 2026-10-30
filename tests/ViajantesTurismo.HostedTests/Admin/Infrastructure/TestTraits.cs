namespace ViajantesTurismo.HostedTests.Admin.Infrastructure;

/// <summary>Trait names used to categorize hosted infrastructure tests.</summary>
internal static class TestTraits
{
    /// <summary>Category for tests that need a real database.</summary>
    public const string DatabaseIntegrationCategory = "database-integration";
    /// <summary>Capability trait for payment tests.</summary>
    public const string PaymentsCapability = "Payments";
    /// <summary>Capability trait for messaging migration tests.</summary>
    public const string MessagingMigrationCapability = "messaging-migration";
    /// <summary>Capability trait for database initialization tests.</summary>
    public const string DatabaseInitializationCapability = "database-initialization";

}
