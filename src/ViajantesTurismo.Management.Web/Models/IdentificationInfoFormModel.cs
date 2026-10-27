using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Management.Web.Models;

/// <summary>
/// Form model for customer identification document details.
/// </summary>
internal sealed class IdentificationInfoFormModel
{
    /// <summary>
    /// National identification number.
    /// </summary>
    [Required(ErrorMessage = "National ID is required")]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string NationalId { get; set; } = string.Empty;

    /// <summary>
    /// Nationality that issued the identification document.
    /// </summary>
    [Required(ErrorMessage = "ID nationality is required")]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string IdNationality { get; set; } = string.Empty;

    /// <summary>
    /// Converts the validated form to an identification info DTO.
    /// </summary>
    public IdentificationInfoDto ToDto() => new()
    {
        NationalId = NationalId,
        IdNationality = IdNationality
    };

    /// <summary>
    /// Creates a form model from an identification info DTO.
    /// </summary>
    public static IdentificationInfoFormModel FromDto(IdentificationInfoDto dto) => new()
    {
        NationalId = dto.NationalId,
        IdNationality = dto.IdNationality
    };
}
