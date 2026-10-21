using System.Text.Json;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class MediaAccessibilityEvaluationEvidence
{
    internal static string[] Dimensions { get; } = ["accessibility", "grounding", "concision", "localization", "unsupported_claims"];

    internal static IReadOnlyList<bool> Evaluate(JsonElement fixtures, JsonElement baseline, JsonElement candidate, bool allowReference = false)
    {
        if (candidate.GetProperty("fixture_version").GetString() != fixtures.GetProperty("version").GetString())
        {
            throw new InvalidDataException("Fixture version mismatch.");
        }

        foreach (var field in new[] { "provider", "model", "prompt_version", "config_version" })
        {
            if (!candidate.TryGetProperty(field, out var value) || string.IsNullOrWhiteSpace(value.GetString()))
            {
                throw new InvalidDataException("Generation provenance is incomplete.");
            }

            if (!allowReference && value.GetString() is "reference-fixture" or "not-applicable" or "reference-v1" or "rubric-v1" or "unreported" or "unconfigured" or "unknown" or "none")
            {
                throw new InvalidDataException("Candidate provenance must identify an actual generation run.");
            }
        }

        var cases = fixtures.GetProperty("cases").EnumerateArray().ToArray();
        var rows = candidate.GetProperty("cases").EnumerateArray().ToArray();
        if (cases.Length != rows.Length || rows.Select(row => row.GetProperty("id").GetString()).Distinct(StringComparer.Ordinal).Count() != cases.Length)
        {
            throw new InvalidDataException("Every golden case must appear exactly once.");
        }

        var results = new List<bool>();
        foreach (var fixture in cases)
        {
            var id = fixture.GetProperty("id").GetString();
            var matchingRows = rows.Where(row => row.GetProperty("id").GetString() == id).ToArray();
            if (matchingRows.Length != 1)
            {
                throw new InvalidDataException("Golden case is missing.");
            }

            var row = matchingRows[0];
            if (!row.TryGetProperty("reviewer", out var reviewer) || string.IsNullOrWhiteSpace(reviewer.GetString())
                || !row.TryGetProperty("reviewed_at", out var reviewedAt) || string.IsNullOrWhiteSpace(reviewedAt.GetString()))
            {
                throw new InvalidDataException("Human rubric review is required.");
            }

            if (!allowReference && reviewer.GetString() == "reference-fixture-author")
            {
                throw new InvalidDataException("Candidate requires an actual human review, not reference scores.");
            }

            var reference = baseline.GetProperty("cases").EnumerateArray().Single(item => item.GetProperty("id").GetString() == id);
            var scores = row.GetProperty("scores");
            if (scores.EnumerateObject().Count() != Dimensions.Length)
            {
                throw new InvalidDataException("Rubric is incomplete.");
            }

            var passed = true;
            foreach (var dimension in Dimensions)
            {
                if (!scores.TryGetProperty(dimension, out var value) || !value.TryGetInt32(out var score) || score is < 0 or > 2)
                {
                    throw new InvalidDataException("Rubric scores must be integers from 0 to 2.");
                }

                passed &= score >= 1 && score >= reference.GetProperty("scores").GetProperty(dimension).GetInt32();
            }

            var alt = row.GetProperty("alt_text").GetString() ?? string.Empty;
            var caption = row.TryGetProperty("caption", out var captionValue) ? captionValue.GetString() : null;
            var combined = string.Concat(alt, " ", caption);
            passed &= !string.IsNullOrWhiteSpace(alt) && alt.Length <= 160;
            passed &= scores.GetProperty("unsupported_claims").GetInt32() == 2;
            passed &= fixture.GetProperty("required_terms").EnumerateArray().All(term => alt.Contains(term.GetString() ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            passed &= !fixture.GetProperty("forbidden_terms").EnumerateArray().Any(term => combined.Contains(term.GetString() ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            results.Add(passed);
        }

        return results;
    }
}
