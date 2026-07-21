using SharedKernel.Results;

namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Creates expected document-lineage failures.</summary>
public static class DocumentLineageErrors
{
    /// <summary>Returns a failure when the booking identifier is missing.</summary>
    public static Result<DocumentLineage> BookingIdRequired() =>
        Result.Invalid(
            detail: "bookingId is required.",
            field: "bookingId",
            message: "bookingId is required.").ConvertError<DocumentLineage>();

    /// <summary>Returns a failure when the document type is invalid.</summary>
    public static Result<DocumentLineage> InvalidDocumentType() =>
        Result.Invalid(
            detail: "documentType has an invalid value.",
            field: "documentType",
            message: "documentType has an invalid value.").ConvertError<DocumentLineage>();

    /// <summary>Returns a conflict when a revision would not advance finalization history.</summary>
    public static Result FinalizedRevisionMustAdvance(int revision, int highestFinalizedRevision) => Result.Conflict(
        detail: $"Document revision {revision} cannot finalize because revision {highestFinalizedRevision} has already finalized.");
}
