namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Defines the raw emergency contact and medical values of a customer.
/// </summary>
/// <param name="EmergencyContactName">The emergency contact name.</param>
/// <param name="EmergencyContactMobile">The emergency contact mobile phone number.</param>
/// <param name="Allergies">The optional allergies.</param>
/// <param name="AdditionalInfo">The optional additional medical information.</param>
public sealed record CustomerHealthDefinition(
    string? EmergencyContactName,
    string? EmergencyContactMobile,
    string? Allergies,
    string? AdditionalInfo);
