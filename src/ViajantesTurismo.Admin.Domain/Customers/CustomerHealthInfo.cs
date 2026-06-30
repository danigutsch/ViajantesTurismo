namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Groups the customer emergency contact and medical information.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CustomerHealthInfo"/> class.
/// </remarks>
/// <param name="emergencyContact">The emergency contact information.</param>
/// <param name="medicalInfo">The medical information and allergies.</param>
public sealed class CustomerHealthInfo(EmergencyContact emergencyContact, MedicalInfo medicalInfo)
{

    /// <summary>Emergency contact information.</summary>
    public EmergencyContact EmergencyContact { get; } = emergencyContact;

    /// <summary>Medical information and allergies.</summary>
    public MedicalInfo MedicalInfo { get; } = medicalInfo;
}
