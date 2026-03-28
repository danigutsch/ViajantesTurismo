using JetBrains.Annotations;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Common.Results;
using ViajantesTurismo.Common.Sanitizers;
using static ViajantesTurismo.Admin.Domain.Customers.CustomerErrors;

namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Represents medical information and allergies.
/// </summary>
public sealed class MedicalInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MedicalInfo"/> class.
    /// </summary>
    /// <param name="allergies">The allergies.</param>
    /// <param name="additionalInfo">The additional medical information.</param>
    private MedicalInfo(string? allergies, string? additionalInfo)
    {
        Allergies = allergies;
        AdditionalInfo = additionalInfo;
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
#pragma warning disable CS8618
    [UsedImplicitly]
    private MedicalInfo()
    {
    }

    /// <summary>Allergies.</summary>
    public string? Allergies { get; private set; }

    /// <summary>Additional medical information.</summary>
    public string? AdditionalInfo { get; private set; }

    /// <summary>
    /// Creates a new instance of <see cref="MedicalInfo"/> with validation.
    /// </summary>
    /// <param name="allergies">The allergies.</param>
    /// <param name="additionalInfo">The additional medical information.</param>
    /// <returns>A <see cref="Result{MedicalInfo}"/> containing the medical info or validation errors.</returns>
    public static Result<MedicalInfo> Create(string? allergies, string? additionalInfo)
    {
        var sanitizedAllergies = StringSanitizer.Sanitize(allergies);
        sanitizedAllergies = string.IsNullOrWhiteSpace(sanitizedAllergies) ? null : sanitizedAllergies;
        var sanitizedAdditionalInfo = StringSanitizer.Sanitize(additionalInfo);
        sanitizedAdditionalInfo = string.IsNullOrWhiteSpace(sanitizedAdditionalInfo) ? null : sanitizedAdditionalInfo;

        var errors = new ValidationErrors();

        if (sanitizedAllergies?.Length > ContractConstants.MaxMedicalInfoLength)
        {
            errors.Add(AllergiesTooLong());
        }

        if (sanitizedAdditionalInfo?.Length > ContractConstants.MaxMedicalInfoLength)
        {
            errors.Add(AdditionalInfoTooLong());
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<MedicalInfo>();
        }

        return new MedicalInfo(sanitizedAllergies, sanitizedAdditionalInfo);
    }
}
