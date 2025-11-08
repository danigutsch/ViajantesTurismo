using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Web.Models;

internal sealed class IdentificationInfoFormModel
{
    [Required(ErrorMessage = "National ID is required")]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string NationalId { get; set; } = string.Empty;

    [Required(ErrorMessage = "ID nationality is required")]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string IdNationality { get; set; } = string.Empty;

    public IdentificationInfoDto ToDto() => new()
    {
        NationalId = NationalId,
        IdNationality = IdNationality
    };

    public static IdentificationInfoFormModel FromDto(IdentificationInfoDto dto) => new()
    {
        NationalId = dto.NationalId,
        IdNationality = dto.IdNationality
    };
}