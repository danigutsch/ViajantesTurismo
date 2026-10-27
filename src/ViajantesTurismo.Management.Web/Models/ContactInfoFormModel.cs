using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Management.Web.Models;

/// <summary>
/// Form model for customer contact details.
/// </summary>
internal sealed class ContactInfoFormModel
{
    /// <summary>
    /// Email address; required and validated.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Mobile phone number; required and validated.
    /// </summary>
    [Required(ErrorMessage = "Mobile is required")]
    [Phone(ErrorMessage = "Invalid phone number")]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string Mobile { get; set; } = string.Empty;

    /// <summary>
    /// Optional Instagram handle.
    /// </summary>
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string? Instagram
    {
        get; set;
    }

    /// <summary>
    /// Optional Facebook profile.
    /// </summary>
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string? Facebook
    {
        get; set;
    }

    /// <summary>
    /// Converts the validated form to a contact info DTO.
    /// </summary>
    public ContactInfoDto ToDto() => new()
    {
        Email = Email,
        Mobile = Mobile,
        Instagram = Instagram,
        Facebook = Facebook
    };

    /// <summary>
    /// Creates a form model from a contact info DTO.
    /// </summary>
    public static ContactInfoFormModel FromDto(ContactInfoDto dto) => new()
    {
        Email = dto.Email,
        Mobile = dto.Mobile,
        Instagram = dto.Instagram,
        Facebook = dto.Facebook
    };
}
