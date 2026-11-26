using ViajantesTurismo.MigrationService.GovernedMigrations;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

public sealed class MigrationPostgreSqlSettingsTests
{
    [Fact]
    public void ConnectionSettings_accepts_legacy_multiplexing_and_preserves_dedicated_sessions()
    {
        // Arrange
        const string owner = "Host=migration.test;Database=fixture;Username=owner;Password='fixture;owner';Multiplexing=true;Pooling=true;Enlist=true";
        const string verifier = "Host=migration.test;Database=fixture;Username=verifier;Multiplexing=false;Pooling=true;Enlist=true";

        // Act
        var (ownerSettings, verifierSettings) = MigrationPostgreSql.ConnectionSettings(owner, verifier);

        // Assert
        ownerSettings.Password.ShouldBe("fixture;owner");
        ownerSettings.Pooling.ShouldBeFalse();
        verifierSettings.Pooling.ShouldBeFalse();
        ownerSettings.Enlist.ShouldBeFalse();
        verifierSettings.Enlist.ShouldBeFalse();
        ownerSettings.ConnectionString.ShouldNotContain("Multiplexing", StringComparison.OrdinalIgnoreCase);
        verifierSettings.ConnectionString.ShouldNotContain("Multiplexing", StringComparison.OrdinalIgnoreCase);
    }
}
