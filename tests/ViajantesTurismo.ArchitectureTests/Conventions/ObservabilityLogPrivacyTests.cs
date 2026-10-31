using PandaBear.Testing;
using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(TestTraitNames.CategoryName, TestTraits.DocumentationCategory)]
public sealed class ObservabilityLogPrivacyTests
{
    [Theory]
    [InlineData("logging.IncludeFormattedMessage = false;")]
    [InlineData("logging.IncludeScopes = false;")]
    [InlineData("logging.AddProcessor(_ => new LogRecordPrivacyProcessor(TelemetryPrivacyAttributeClassifier.IsSensitive));")]
    public void Service_defaults_keep_exported_logs_free_of_formatted_messages_scopes_and_sensitive_attributes(string required)
    {
        // Arrange
        var source = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(), "src", "ViajantesTurismo.ServiceDefaults", "ServiceDefaultsExtensions.cs"));

        // Act
        var contains = source.Contains(required, StringComparison.Ordinal);

        // Assert
        contains.ShouldBeTrue();
    }
}
