using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Management.Web.Models;

/// <summary>
/// Form model for customer personal details.
/// </summary>
internal sealed class PersonalInfoFormModel
{
    /// <summary>
    /// Given name.
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    [MaxLength(ContractConstants.MaxNameLength)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Family name.
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(ContractConstants.MaxNameLength)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Date of birth; required.
    /// </summary>
    [Required(ErrorMessage = "Birth date is required")]
    public DateOnly? BirthDate
    {
        get; set;
    }

    /// <summary>
    /// Gender as free text.
    /// </summary>
    [Required(ErrorMessage = "Gender is required")]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string Gender { get; set; } = string.Empty;

    /// <summary>
    /// Nationality.
    /// </summary>
    [Required(ErrorMessage = "Nationality is required")]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string Nationality { get; set; } = string.Empty;

    /// <summary>
    /// Occupation.
    /// </summary>
    [Required(ErrorMessage = "Occupation is required")]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string Occupation { get; set; } = string.Empty;

    /// <summary>
    /// Converts the validated form to a personal info DTO.
    /// </summary>
    public PersonalInfoDto ToDto() => new()
    {
        FirstName = FirstName,
        LastName = LastName,
        BirthDate = BirthDate!.Value,
        Gender = Gender,
        Nationality = Nationality,
        Occupation = Occupation
    };

    /// <summary>
    /// Creates a form model from a personal info DTO.
    /// </summary>
    public static PersonalInfoFormModel FromDto(PersonalInfoDto dto) => new()
    {
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        BirthDate = dto.BirthDate,
        Gender = dto.Gender,
        Nationality = dto.Nationality,
        Occupation = dto.Occupation
    };
}
