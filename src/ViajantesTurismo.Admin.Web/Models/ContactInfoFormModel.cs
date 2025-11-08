using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Web.Models;

internal sealed class ContactInfoFormModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile is required")]
    [Phone(ErrorMessage = "Invalid phone number")]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string Mobile { get; set; } = string.Empty;

    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string? Instagram { get; set; }

    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string? Facebook { get; set; }

    public ContactInfoDto ToDto() => new()
    {
        Email = Email,
        Mobile = Mobile,
        Instagram = Instagram,
        Facebook = Facebook
    };

    public static ContactInfoFormModel FromDto(ContactInfoDto dto) => new()
    {
        Email = dto.Email,
        Mobile = dto.Mobile,
        Instagram = dto.Instagram,
        Facebook = dto.Facebook
    };
}