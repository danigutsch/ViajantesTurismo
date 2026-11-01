namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Identifies which source record a document is generated from.</summary>
public enum DocumentSourceKind
{
    /// <summary>The booking the document belongs to.</summary>
    Booking,

    /// <summary>The tour the booking belongs to.</summary>
    Tour,
}
