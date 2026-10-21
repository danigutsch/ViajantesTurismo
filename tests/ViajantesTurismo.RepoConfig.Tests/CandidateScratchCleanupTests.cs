using System.Text.Json.Nodes;

namespace ViajantesTurismo.RepoConfig.Tests;

[Trait(SharedKernelTestTraitNames.CapabilityName, "PackageConsumption")]
public sealed class CandidateScratchCleanupTests
{
    [Fact]
    public async Task Failed_restore_persists_evidence_and_removes_its_isolated_scratch_directory()
    {
        // Arrange
        using var scope = new CandidateScratchTestScope();
        var ct = TestContext.Current.CancellationToken;
        await scope.Prepare(ct);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var destination = Path.Combine(scope.Candidate.Root, "evidence", "compatibility.json");

        // Act
        var result = await CandidateCompatibilityCommand.Run(
            ["--candidate", scope.Candidate.Manifest, "--output", destination], output, error, scope.Candidate.Root, ct);

        // Assert
        result.ShouldBe(1);
        var evidence = JsonNode.Parse(await File.ReadAllTextAsync(destination, ct)).ShouldNotBeNull();
        evidence["consumerCompatibility"].ShouldNotBeNull().GetValue<string>().ShouldBe("failed");
        evidence["reason"].ShouldNotBeNull().GetValue<string>().ShouldContain("dotnet exited", StringComparison.Ordinal);
        scope.FindScratchDirectories().ShouldBeEmpty();
    }

    [Fact]
    public async Task Cancellation_stops_the_cache_writer_before_persisting_failure_and_deleting_scratch()
    {
        // Arrange
        await using var scope = new CandidateScratchTestScope();
        var ct = TestContext.Current.CancellationToken;
        var destination = Path.Combine(scope.Candidate.Root, "evidence", "compatibility.json");
        using var child = await scope.StartBlockedRestore(destination, ct);
        var scratch = scope.FindScratchDirectories().ShouldHaveSingleItem();
        File.Exists(Path.Combine(scratch, "packages", "held.cache")).ShouldBeTrue();

        // Act
        await scope.Cancel();
        Func<Task> complete = () => scope.Completion;
        await complete.ShouldThrow<OperationCanceledException>();

        // Assert
        child.HasExited.ShouldBeTrue();
        Directory.Exists(scratch).ShouldBeFalse();
        var evidence = JsonNode.Parse(await File.ReadAllTextAsync(destination, ct)).ShouldNotBeNull();
        evidence["consumerCompatibility"].ShouldNotBeNull().GetValue<string>().ShouldBe("failed");
        evidence["reason"].ShouldNotBeNull().GetValue<string>().ShouldNotBeEmpty();
    }
}
