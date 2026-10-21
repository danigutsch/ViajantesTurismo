using System.Text.Json;
using System.Text.Json.Nodes;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using ViajantesTurismo.Catalog.Application;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.CatalogArea)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.ScopeName, PandaBear.Testing.SharedKernelTestTraitNames.UnitScope)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.AiAccessibilityCapability)]
[Collection("Catalog telemetry")]
public sealed class MediaAccessibilityEvaluationTests
{
    [Fact]
    public void Golden_reference_and_optional_reviewed_candidate_meet_rubric_without_regression()
    {
        // Arrange
        var directory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "media-accessibility");
        using var fixtures = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "golden.json")));
        using var baseline = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "baseline.json")));
        var configuredCandidate = Environment.GetEnvironmentVariable("MEDIA_ACCESSIBILITY_EVALUATION_CANDIDATE");
        var candidatePath = configuredCandidate ?? Path.Combine(directory, "baseline.json");
        using var candidate = JsonDocument.Parse(File.ReadAllText(candidatePath));
        var telemetryBuilder = Sdk.CreateMeterProviderBuilder().AddMeter(CatalogTelemetry.Name);
        var endpoint = Environment.GetEnvironmentVariable("MEDIA_ACCESSIBILITY_EVALUATION_OTLP_ENDPOINT");
        if (!string.IsNullOrWhiteSpace(endpoint) && configuredCandidate is not null)
        {
            var uri = new Uri(endpoint, UriKind.Absolute);
            if (uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            {
                throw new InvalidDataException("OTLP endpoint must be HTTP(S) without credentials, query or fragment.");
            }

            telemetryBuilder.AddOtlpExporter(options =>
            {
                options.Endpoint = uri;
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            });
        }

        using var provider = telemetryBuilder.Build();

        foreach (var fixture in fixtures.RootElement.GetProperty("cases").EnumerateArray())
        {
            var imageName = fixture.GetProperty("image").GetString();
            imageName.ShouldNotBeNull();
            var bytes = File.ReadAllBytes(Path.Combine(directory, imageName));
            var hash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes));
            hash.ShouldBe(fixture.GetProperty("sha256").GetString());
        }

        // Act
        var results = MediaAccessibilityEvaluationEvidence.Evaluate(fixtures.RootElement, baseline.RootElement, candidate.RootElement, allowReference: configuredCandidate is null);
        if (configuredCandidate is not null)
        {
            foreach (var passed in results)
            {
                MediaAccessibilityTelemetry.RecordEvaluation(passed);
            }
        }

        // Assert
        results.All(passed => passed).ShouldBeTrue();
    }

    [Fact]
    public void Reference_scores_cannot_masquerade_as_a_model_evaluation()
    {
        // Arrange
        var directory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "media-accessibility");
        using var fixtures = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "golden.json")));
        using var baseline = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "baseline.json")));

        // Act
        Action action = () => MediaAccessibilityEvaluationEvidence.Evaluate(fixtures.RootElement, baseline.RootElement, baseline.RootElement);

        // Assert
        action.ShouldThrow<InvalidDataException>();
    }

    [Theory]
    [InlineData("model")]
    [InlineData("provider")]
    [InlineData("prompt_version")]
    [InlineData("config_version")]
    public void Missing_generation_provenance_rejects_evidence(string field)
    {
        // Arrange
        var directory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "media-accessibility");
        using var fixtures = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "golden.json")));
        using var baseline = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "baseline.json")));
        var candidate = JsonNode.Parse(baseline.RootElement.GetRawText());
        candidate.ShouldNotBeNull();
        candidate.AsObject().Remove(field);
        using var evidence = JsonDocument.Parse(candidate.ToJsonString());

        // Act
        Action action = () => MediaAccessibilityEvaluationEvidence.Evaluate(fixtures.RootElement, baseline.RootElement, evidence.RootElement, allowReference: true);

        // Assert
        action.ShouldThrow<InvalidDataException>();
    }

    [Theory]
    [InlineData("unsupported_claims", 1)]
    [InlineData("localization", 0)]
    [InlineData("grounding", 1)]
    public void Rubric_regressions_fail_evaluation(string dimension, int score)
    {
        // Arrange
        var directory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "media-accessibility");
        using var fixtures = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "golden.json")));
        using var baseline = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "baseline.json")));
        var candidate = JsonNode.Parse(baseline.RootElement.GetRawText());
        candidate.ShouldNotBeNull();
        var cases = candidate["cases"];
        cases.ShouldNotBeNull();
        var first = cases[0];
        first.ShouldNotBeNull();
        var scores = first["scores"];
        scores.ShouldNotBeNull();
        scores[dimension] = score;
        using var evidence = JsonDocument.Parse(candidate.ToJsonString());

        // Act
        var results = MediaAccessibilityEvaluationEvidence.Evaluate(fixtures.RootElement, baseline.RootElement, evidence.RootElement, allowReference: true);

        // Assert
        results[0].ShouldBeFalse();
    }

    [Theory]
    [InlineData("Copacabana beach with blue sea.")]
    [InlineData("A wheelchair accessible beach beside a blue sea.")]
    [InlineData("Mar azul junto a uma praia de areia.")]
    public void Unsupported_claims_and_wrong_language_fail_even_when_human_scores_claim_success(string altText)
    {
        // Arrange
        var directory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "media-accessibility");
        using var fixtures = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "golden.json")));
        using var baseline = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "baseline.json")));
        var candidate = JsonNode.Parse(baseline.RootElement.GetRawText());
        candidate.ShouldNotBeNull();
        var cases = candidate["cases"];
        cases.ShouldNotBeNull();
        var first = cases[0];
        first.ShouldNotBeNull();
        first["alt_text"] = altText;
        using var evidence = JsonDocument.Parse(candidate.ToJsonString());

        // Act
        var results = MediaAccessibilityEvaluationEvidence.Evaluate(fixtures.RootElement, baseline.RootElement, evidence.RootElement, allowReference: true);

        // Assert
        results[0].ShouldBeFalse();
    }
}
