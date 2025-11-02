using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Admin.Web.Models;

internal sealed class PersonalInfoFormModel
{
    [Required(ErrorMessage = "First name is required")]
    [MaxLength(ContractConstants.MaxNameLength)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(ContractConstants.MaxNameLength)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Birth date is required")]
    public DateTime? BirthDate { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nationality is required")]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string Nationality { get; set; } = string.Empty;

    [Required(ErrorMessage = "Profession is required")]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string Profession { get; set; } = string.Empty;

    public PersonalInfoDto ToDto() => new()
    {
        FirstName = FirstName,
        LastName = LastName,
        BirthDate = BirthDate!.Value,
        Gender = Gender,
        Nationality = Nationality,
        Profession = Profession
    };

    public static PersonalInfoFormModel FromDto(PersonalInfoDto dto) => new()
    {
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        BirthDate = dto.BirthDate,
        Gender = dto.Gender,
        Nationality = dto.Nationality,
        Profession = dto.Profession
    };
}