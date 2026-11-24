using System.Text.Json;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Checks that each repository client uses the pinned Aspire stdio launcher.</summary>
public sealed class AspireMcpConfigurationTests
{
    [Theory]
    [InlineData(".mcp.json", "mcpServers")]
    [InlineData(".vscode/mcp.json", "servers")]
    [InlineData("opencode.json", "mcp")]
    public void Aspire_resource_discovery_uses_the_repository_local_tool(string configurationFile, string serversProperty)
    {
        // Arrange
        var root = RepositoryRoot.Locate("ViajantesTurismo.slnx");
        using var configuration = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, configurationFile)));
        using var tools = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".config", "dotnet-tools.json")));

        // Act
        var aspire = configuration.RootElement.GetProperty(serversProperty).GetProperty("aspire");
        var executable = aspire.GetProperty("command");
        var invocation = executable.ValueKind == JsonValueKind.Array
            ? executable.EnumerateArray().Select(static argument => argument.GetString()).ToArray()
            : [executable.GetString(), .. aspire.GetProperty("args").EnumerateArray().Select(static argument => argument.GetString())];
        var pinnedCommands = tools.RootElement.GetProperty("tools").GetProperty("aspire.cli").GetProperty("commands")
            .EnumerateArray().Select(static command => command.GetString()).ToArray();

        // Assert
        invocation.ShouldBe(["dotnet", "tool", "run", "aspire", "--", "agent", "mcp"]);
        pinnedCommands.ShouldHaveSingleItem().ShouldBe("aspire");
        aspire.TryGetProperty("env", out _).ShouldBeFalse();
        aspire.TryGetProperty("environment", out _).ShouldBeFalse();
    }
}
