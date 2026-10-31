using PandaBear.Results;

namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Provides expected validation failures for draft and idempotency purge evidence.</summary>
public static class DocumentRetentionPurgeErrors
{
    /// <summary>Returns a failure for a purge evidence timestamp that is not UTC.</summary>
    public static Result InvalidOccurredAtUtc() => Result.Invalid(
        detail: "A purge evidence timestamp must be UTC.",
        field: "occurredAtUtc",
        message: "The purge evidence timestamp must be UTC.");

    /// <summary>Returns a failure for purge evidence with an undefined kind or outcome, negative counts, or counts on a refused run.</summary>
    public static Result InvalidEvidence() => Result.Invalid(
        detail: "Purge evidence must use a defined kind and outcome, non-negative counts, and no counts for a refused run.",
        field: "purgeEvidence",
        message: "The purge evidence is invalid.");
}
