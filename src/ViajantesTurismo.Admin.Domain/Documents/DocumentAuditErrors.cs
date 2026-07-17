using SharedKernel.Results;

namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Provides expected validation failures for document audit records.</summary>
public static class DocumentAuditErrors
{
    /// <summary>Returns a failure when a document mutation has no trusted audit context.</summary>
    public static Result AuditContextRequired() => Result.Invalid(
        detail: "A trusted document audit context is required.",
        field: "auditContext",
        message: "Document operations require audit context.");

    /// <summary>Returns a failure when document audit persistence is unavailable.</summary>
    public static Result AuditStoreUnavailable() => Result.Unavailable(
        detail: "Document audit persistence is unavailable.");

    /// <summary>Returns a failure for a missing or invalid opaque actor identifier.</summary>
    public static Result InvalidActorId() => Result.Invalid(
        detail: "A document audit record requires an opaque actor identifier.",
        field: "actorId",
        message: "An actor identifier is required.");

    /// <summary>Returns a failure for a missing or invalid correlation identifier.</summary>
    public static Result InvalidCorrelationId() => Result.Invalid(
        detail: "A document audit record requires a server-generated correlation identifier.",
        field: "correlationId",
        message: "A correlation identifier is required.");

    /// <summary>Returns a failure for an invalid audit timestamp.</summary>
    public static Result InvalidOccurredAtUtc() => Result.Invalid(
        detail: "A document audit timestamp must be UTC.",
        field: "occurredAtUtc",
        message: "The audit timestamp must be UTC.");

    /// <summary>Returns a failure for an invalid opaque resource identifier.</summary>
    public static Result InvalidResourceId() => Result.Invalid(
        detail: "A document audit resource identifier cannot be empty.",
        field: "resourceId",
        message: "The resource identifier is invalid.");

    /// <summary>Returns a failure for an invalid document revision.</summary>
    public static Result InvalidDocumentRevision() => Result.Invalid(
        detail: "A document audit revision must be greater than zero when present.",
        field: "documentRevision",
        message: "The document revision is invalid.");
}
