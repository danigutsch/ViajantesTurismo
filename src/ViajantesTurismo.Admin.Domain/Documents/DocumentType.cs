namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>
/// Identifies a generated travel document type.
/// </summary>
public enum DocumentType
{
    /// <summary>
    /// Customer-facing booking confirmation and service contract.
    /// </summary>
    BookingConfirmationContract = 0,

    /// <summary>Frozen payment instructions backed by approved financial terms and a verified recipient revision.</summary>
    PaymentInstructions = 1
}
