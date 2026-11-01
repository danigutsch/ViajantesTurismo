using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>The outcome of reading a document's current source version, naming the missing source on a failure.</summary>
/// <param name="Result">The source version, or the failure to read it.</param>
/// <param name="MissingSource">The source that was not found, or <see langword="null" /> when none was missing.</param>
public sealed record DocumentSourceVersionRead(Result<string> Result, DocumentSourceKind? MissingSource = null)
{
    /// <summary>Gets the audit reason code that records a failed read.</summary>
    public DocumentAuditReasonCode AuditReasonCode => MissingSource == DocumentSourceKind.Tour
        ? DocumentAuditReasonCode.TourNotFound
        : DocumentAuditReasonCode.BookingNotFound;

    /// <summary>Creates a successful read.</summary>
    /// <param name="version">The current source version.</param>
    public static DocumentSourceVersionRead Found(string version) => new(PandaBear.Results.Result.Ok(version));

    /// <summary>Creates a failed read for a source record that was not found.</summary>
    /// <param name="source">The source that was missing.</param>
    /// <param name="error">The not-found failure.</param>
    public static DocumentSourceVersionRead Missing(DocumentSourceKind source, Result error) =>
        new(error.ConvertError<string>(), source);
}
