using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Web.Models;

internal sealed class MedicalInfoFormModel
{
    [MaxLength(ContractConstants.MaxServiceDescriptionLength)]
    public string? Allergies { get; set; }

    [MaxLength(ContractConstants.MaxServiceDescriptionLength)]
    public string? AdditionalInfo { get; set; }

    public MedicalInfoDto ToDto() => new()
    {
        Allergies = Allergies,
        AdditionalInfo = AdditionalInfo
    };

    public static MedicalInfoFormModel FromDto(MedicalInfoDto dto) => new()
    {
        Allergies = dto.Allergies,
        AdditionalInfo = dto.AdditionalInfo
    };
}
