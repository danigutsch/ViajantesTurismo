namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>
/// Classifies a document field before it can be persisted or rendered.
/// </summary>
public enum DocumentPrivacyClassification
{
    /// <summary>No classification was assigned.</summary>
    Unclassified = 0,

    /// <summary>Public customer-safe content.</summary>
    Public = 1,

    /// <summary>Operational content requiring normal staff handling.</summary>
    Operational = 2,

    /// <summary>Personal data.</summary>
    PersonalData = 3,

    /// <summary>Sensitive personal data.</summary>
    SensitivePersonalData = 4,

    /// <summary>Secret material that must not be rendered in this document type.</summary>
    Secret = 5
}
