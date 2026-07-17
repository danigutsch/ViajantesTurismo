using SharedKernel.Results;

namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>
/// Creates expected generated-document failures.
/// </summary>
public static class DocumentErrors
{
    /// <summary>Returns a document-not-found failure.</summary>
    public static Result DocumentNotFound(Guid id) => Result.NotFound(detail: $"Document with ID {id} was not found.");

    /// <summary>Returns a failure for a booking that cannot produce a customer-facing document draft.</summary>
    public static Result BookingIsNotAccepted() => Result.Conflict(
        detail: "A customer-facing document draft requires a confirmed or completed booking.");

    /// <summary>Returns an unclassified-field failure.</summary>
    public static Result UnclassifiedField(string fieldId) => Result.Invalid(
        detail: $"Document field '{fieldId}' must have a privacy classification.",
        field: "fields",
        message: "Document fields must be classified.");

    /// <summary>Returns a secret-field failure.</summary>
    public static Result SecretFieldCannotBeRendered(string fieldId) => Result.Invalid(
        detail: $"Document field '{fieldId}' contains secret material and cannot be rendered.",
        field: "fields",
        message: "Secret document fields cannot be rendered.");

    /// <summary>Returns a non-editable-field failure.</summary>
    public static Result FieldIsNotEditable(string fieldId) => Result.Conflict(
        detail: $"Document field '{fieldId}' is not editable.");

    /// <summary>Returns an unknown-field failure.</summary>
    public static Result FieldNotFound(string fieldId) => Result.NotFound(
        detail: $"Document field '{fieldId}' was not found.");

    /// <summary>Returns a duplicate-field failure.</summary>
    public static Result DuplicateFieldId(string fieldId) => Result.Invalid(
        detail: $"Document field '{fieldId}' is duplicated.",
        field: "fields",
        message: "Document field identifiers must be unique.");

    /// <summary>Returns an invalid document-state transition failure.</summary>
    public static Result InvalidStatusTransition(DocumentStatus current, DocumentStatus target) => Result.Conflict(
        detail: $"Cannot transition document from {current} to {target}.");

    /// <summary>Returns an immutable-document failure.</summary>
    public static Result DocumentIsImmutable(DocumentStatus status) => Result.Conflict(
        detail: $"A {status} document revision is immutable.");

    /// <summary>Returns a missing-void-reason failure.</summary>
    public static Result VoidReasonRequired() => Result.Invalid(
        detail: "A reason is required to void a document.",
        field: "reason",
        message: "A void reason is required.");

    /// <summary>Returns an invalid artifact-content failure.</summary>
    public static Result ArtifactContentRequired() => Result.Invalid(
        detail: "Finalized artifact content is required.",
        field: "artifact",
        message: "Artifact content is required.");

    /// <summary>Returns a failure when a document has no sealed artifact available for download.</summary>
    public static Result FinalizedArtifactNotAvailable() => Result.Conflict(
        detail: "A finalized document artifact is not available.");

    /// <summary>Returns a conflict when a concurrent request changed the document revision.</summary>
    public static Result DocumentChangedByAnotherRequest() => Result.Conflict(
        detail: "The document was changed by another request. Reload and retry.");

    /// <summary>Returns a length-validation failure.</summary>
    public static Result ValueTooLong(string field, int maxLength) => Result.Invalid(
        detail: $"{field} cannot exceed {maxLength} characters.",
        field: field,
        message: $"{field} cannot exceed {maxLength} characters.");

    /// <summary>Returns an invalid-value failure.</summary>
    public static Result InvalidValue(string field) => Result.Invalid(
        detail: $"{field} has an invalid value.",
        field: field,
        message: $"{field} has an invalid value.");

    /// <summary>Returns an empty-value validation failure.</summary>
    public static Result ValueRequired(string field) => Result.Invalid(
        detail: $"{field} is required.",
        field: field,
        message: $"{field} is required.");
}
