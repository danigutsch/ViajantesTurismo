using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Management.Web.Models;

/// <summary>
/// Form model for customer medical information.
/// </summary>
internal sealed class MedicalInfoFormModel
{
    /// <summary>
    /// Optional description of known allergies.
    /// </summary>
    [MaxLength(ContractConstants.MaxServiceDescriptionLength)]
    public string? Allergies
    {
        get; set;
    }

    /// <summary>
    /// Optional additional medical information.
    /// </summary>
    [MaxLength(ContractConstants.MaxServiceDescriptionLength)]
    public string? AdditionalInfo
    {
        get; set;
    }

    /// <summary>
    /// Converts the form to a medical info DTO.
    /// </summary>
    public MedicalInfoDto ToDto() => new()
    {
        Allergies = Allergies,
        AdditionalInfo = AdditionalInfo
    };

    /// <summary>
    /// Creates a form model from a medical info DTO.
    /// </summary>
    public static MedicalInfoFormModel FromDto(MedicalInfoDto dto) => new()
    {
        Allergies = dto.Allergies,
        AdditionalInfo = dto.AdditionalInfo
    };
}
