using PandaBear.Results;

namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>Creates expected document hold failures. Messages are fixed text and never echo a request value.</summary>
public static class DocumentHoldErrors
{
    /// <summary>Returns a hold-not-found failure, also used for a hold that belongs to another lineage.</summary>
    public static Result HoldNotFound() => Result.NotFound(detail: "Hold was not found.");

    /// <summary>Returns a failure for releasing a hold that was already released.</summary>
    public static Result HoldAlreadyReleased() => Result.Conflict(detail: "The hold was already released.");

    /// <summary>Returns a validation failure for one hold field without echoing its value.</summary>
    /// <param name="field">The request field name.</param>
    public static Result InvalidField(string field) => Result.Invalid(
        detail: "The hold request is not valid.",
        field: field,
        message: "The value is not valid.");
}
