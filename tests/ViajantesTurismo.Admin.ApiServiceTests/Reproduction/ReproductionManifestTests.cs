using System.Text.Json.Nodes;
using System.Text.Json;
using PandaBear.Testing.Snapshots;
using PandaBear.Testing;

namespace ViajantesTurismo.Admin.ApiServiceTests.Reproduction;

/// <summary>Verifies the manifest schema, controlled values, byte limit, and file lifecycle.</summary>
public sealed class ReproductionManifestTests
{
    [Fact]
    public async Task An_approved_attempt_writes_only_the_allowed_JSON_fields()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var directory = new TemporaryDirectory();
        var expected = ReproductionManifestTestData.Create();
        var specification = ReproductionManifestTestData.Specification;

        // Act
        var path = await ReproductionManifestCodec.Write(directory.Path, specification, expected, ct);
        var read = await ReproductionManifestCodec.Read(path, specification, ct);
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path, ct));
        var fields = document.RootElement.EnumerateObject().Select(static property => property.Name).Order(StringComparer.Ordinal).ToArray();

        // Assert
        Path.GetFileName(path).ShouldBe($"{expected.AttemptId:N}.json");
        fields.ShouldBe(["attemptId", "durationMs", "outcomeCode", "resourceStates", "result", "revision", "scenario", "schemaVersion", "startedUtc", "traceIds"]);
        read.AttemptId.ShouldBe(expected.AttemptId);
        read.TraceIds.ShouldBe(expected.TraceIds);
        read.Outcome.ResourceStates.ShouldBe(expected.Outcome.ResourceStates);
    }

    [Theory]
    [InlineData("authorization", "Bearer synthetic-token")]
    [InlineData("cookie", "session=synthetic-authentication-cookie")]
    [InlineData("requestBody", "{\"customer\":\"synthetic@example.invalid\"}")]
    [InlineData("responseBody", "synthetic personal response")]
    [InlineData("messageBody", "synthetic integration payload")]
    [InlineData("browserState", "synthetic storage state")]
    [InlineData("revision", "Bearer synthetic-token")]
    [InlineData("scenario", "synthetic@example.invalid")]
    [InlineData("result", "session=synthetic-authentication-cookie")]
    [InlineData("outcomeCode", "synthetic personal response")]
    public async Task Headers_bodies_and_unapproved_values_are_refused_without_repeating_them(string field, string value)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var directory = new TemporaryDirectory();
        var specification = ReproductionManifestTestData.Specification;
        var path = await ReproductionManifestCodec.Write(directory.Path, specification, ReproductionManifestTestData.Create(), ct);
        var original = await File.ReadAllTextAsync(path, ct);
        var document = JsonNode.Parse(original).ShouldNotBeNull().AsObject();
        document[field] = value;
        await File.WriteAllTextAsync(path, document.ToJsonString(), ct);

        // Act
        Func<Task> read = async () => await ReproductionManifestCodec.Read(path, specification, ct);

        // Assert
        var error = await read.ShouldThrow<InvalidDataException>();
        error.Message.ShouldNotContain(value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Nested_resource_states_and_trace_IDs_must_be_approved()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var directory = new TemporaryDirectory();
        var specification = ReproductionManifestTestData.Specification;
        var path = await ReproductionManifestCodec.Write(directory.Path, specification, ReproductionManifestTestData.Create(), ct);
        var original = await File.ReadAllTextAsync(path, ct);
        var document = JsonNode.Parse(original).ShouldNotBeNull().AsObject();
        document["resourceStates"] = new JsonObject { ["admin-api"] = "synthetic@example.invalid" };
        await File.WriteAllTextAsync(path, document.ToJsonString(), ct);
        Func<Task> read = async () => await ReproductionManifestCodec.Read(path, specification, ct);

        // Act
        var resourceError = await read.ShouldThrow<InvalidDataException>();
        document["resourceStates"] = new JsonObject { ["admin-api"] = "ready" };
        document["traceIds"] = new JsonArray("Bearer synthetic-token");
        await File.WriteAllTextAsync(path, document.ToJsonString(), ct);
        var traceError = await read.ShouldThrow<InvalidDataException>();

        // Assert
        resourceError.Message.ShouldNotContain("synthetic@example.invalid", StringComparison.Ordinal);
        traceError.Message.ShouldNotContain("synthetic-token", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Duplicate_JSON_fields_and_oversize_input_are_rejected()
    {
        // Arrange
        const int maximumBytes = 64 * 1024;
        var ct = TestContext.Current.CancellationToken;
        using var directory = new TemporaryDirectory();
        var specification = ReproductionManifestTestData.Specification;
        var manifest = ReproductionManifestTestData.Create();
        var path = await ReproductionManifestCodec.Write(directory.Path, specification, manifest, ct);
        var original = await File.ReadAllTextAsync(path, ct);
        Func<Task> read = async () => await ReproductionManifestCodec.Read(path, specification, ct);

        // Act
        await File.WriteAllTextAsync(path, original.Insert(1, "\"schemaVersion\":1,"), ct);
        var duplicateError = await read.ShouldThrow<InvalidDataException>();
        await File.WriteAllTextAsync(path, original.PadRight(maximumBytes), ct);
        var atLimit = await ReproductionManifestCodec.Read(path, specification, ct);
        await File.AppendAllTextAsync(path, " ", ct);
        var sizeError = await read.ShouldThrow<InvalidDataException>();

        // Assert
        duplicateError.Message.ShouldBe(sizeError.Message);
        atLimit.AttemptId.ShouldBe(manifest.AttemptId);
    }

    [Fact]
    public async Task A_rejected_or_cancelled_write_leaves_no_artifact()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var directory = new TemporaryDirectory();
        var specification = ReproductionManifestTestData.Specification;
        var manifest = ReproductionManifestTestData.Create();
        var invalid = manifest with
        {
            Outcome = manifest.Outcome with
            {
                OutcomeCode = "unregistered"
            }
        };

        // Act
        Func<Task> rejectedWrite = async () => await ReproductionManifestCodec.Write(directory.Path, specification, invalid, ct);
        Func<Task> cancelledWrite = async () => await ReproductionManifestCodec.Write(directory.Path, specification, manifest, new CancellationToken(canceled: true));

        // Assert
        await rejectedWrite.ShouldThrow<InvalidDataException>();
        await cancelledWrite.ShouldThrowAssignableTo<OperationCanceledException>();
        Directory.GetFiles(directory.Path).ShouldBeEmpty();
    }

    [Fact]
    public async Task A_reused_attempt_ID_preserves_the_original_file_and_cleans_up()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var directory = new TemporaryDirectory();
        var specification = ReproductionManifestTestData.Specification;
        var manifest = ReproductionManifestTestData.Create();
        var path = await ReproductionManifestCodec.Write(directory.Path, specification, manifest, ct);
        var original = await File.ReadAllTextAsync(path, ct);
        var duplicate = manifest with
        {
            DurationMs = manifest.DurationMs + 1
        };

        // Act
        Func<Task> rewrite = async () => await ReproductionManifestCodec.Write(directory.Path, specification, duplicate, ct);
        await rewrite.ShouldThrow<IOException>();
        var after = await File.ReadAllTextAsync(path, ct);

        // Assert
        after.ShouldBe(original);
        Directory.GetFiles(directory.Path).ShouldBe([path]);
    }
}
