using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Web.Models;

internal sealed class MedicalInfoFormModel
{
    [MaxLength(ContractConstants.MaxServiceDescriptionLength)]
    public string? Allergies { get; set; }

    [MaxLength(ContractConstants.MaxServiceDescriptionLength)]
    public string? AdditionalInfo { get; set; }

    public MedicalInfoStepDto ToDto() => new()
    {
        Allergies = Allergies,
        AdditionalInfo = AdditionalInfo
    };

    public static MedicalInfoFormModel FromDto(MedicalInfoStepDto dto) => new()
    {
        Allergies = dto.Allergies,
        AdditionalInfo = dto.AdditionalInfo
    };
}