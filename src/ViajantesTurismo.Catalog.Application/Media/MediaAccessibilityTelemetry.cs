using System.Diagnostics;
using System.Diagnostics.Metrics;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>Records privacy-safe generation and human review measurements through the Catalog meter.</summary>
public static class MediaAccessibilityTelemetry
{
    private static readonly Counter<long> Attempts = CatalogTelemetry.Meter.CreateCounter<long>("catalog.media_accessibility.generation.attempts", "{attempt}");
    private static readonly Counter<long> Completions = CatalogTelemetry.Meter.CreateCounter<long>("catalog.media_accessibility.generation.completed", "{attempt}");
    private static readonly Histogram<double> Duration = CatalogTelemetry.Meter.CreateHistogram<double>("catalog.media_accessibility.generation.duration", "s");
    private static readonly Counter<long> Evaluations = CatalogTelemetry.Meter.CreateCounter<long>("catalog.media_accessibility.evaluation.cases", "{case}");
    private static readonly Counter<long> PublicationBlockers = CatalogTelemetry.Meter.CreateCounter<long>("catalog.media_accessibility.publication.blocked", "{image}");
    private static readonly Counter<long> Reviews = CatalogTelemetry.Meter.CreateCounter<long>("catalog.media_accessibility.review.decisions", "{decision}");
    private static readonly Histogram<double> QueueAge = CatalogTelemetry.Meter.CreateHistogram<double>("catalog.media_accessibility.review.queue_age", "s");

    /// <summary>Records a review decision without image identifiers or text.</summary>
    /// <param name="outcome">One of approved, edited, rejected, failed, or missing_alt.</param>
    /// <param name="language">Reviewed language.</param>
    /// <param name="queueAgeSeconds">Measured queue age, when known.</param>
    /// <exception cref="ArgumentOutOfRangeException">The outcome or queue age is invalid.</exception>
    public static void RecordReview(string outcome, PublicContentLanguage language, double? queueAgeSeconds = null)
    {
        if (outcome is not ("approved" or "edited" or "rejected" or "failed" or "missing_alt"))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        if (queueAgeSeconds is { } age && (!double.IsFinite(age) || age < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(queueAgeSeconds));
        }

        var tags = Tags(outcome, language);
        Reviews.Add(1, tags);
        if (queueAgeSeconds is { } knownAge)
        {
            QueueAge.Record(knownAge, tags);
        }
    }

    /// <summary>Records a rubric evaluation result without candidate text or provenance details.</summary>
    /// <param name="passed">Whether the case meets the baseline and safety rubric.</param>
    public static void RecordEvaluation(bool passed)
    {
        Evaluations.Add(1, new KeyValuePair<string, object?>("catalog.outcome", passed ? "pass" : "fail"));
    }

    /// <summary>Records an image withheld by the public accessibility delivery gate.</summary>
    /// <param name="missingAltText">Whether non-decorative alternative text is missing.</param>
    public static void RecordPublicationBlocker(bool missingAltText)
    {
        PublicationBlockers.Add(1, new KeyValuePair<string, object?>("catalog.outcome", missingAltText ? "missing_alt" : "unreviewed"));
    }

    internal static void RecordAttempt(PublicContentLanguage language)
    {
        Attempts.Add(1, Tags("started", language));
    }

    internal static void RecordCompletion(string outcome, PublicContentLanguage language, long started)
    {
        var tags = Tags(outcome, language);
        Completions.Add(1, tags);
        Duration.Record(Stopwatch.GetElapsedTime(started).TotalSeconds, tags);
    }

    private static TagList Tags(string outcome, PublicContentLanguage language)
    {
        return new TagList
        {
            { "catalog.outcome", outcome },
            { "catalog.media_accessibility.language", language switch { PublicContentLanguage.EnUs => "en-US", PublicContentLanguage.PtBr => "pt-BR", _ => "und" } }
        };
    }
}
