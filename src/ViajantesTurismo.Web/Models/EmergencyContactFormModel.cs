using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Web.Models;

internal sealed class EmergencyContactFormModel
{
    [Required(ErrorMessage = "Emergency contact name is required")]
    [MaxLength(ContractConstants.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Emergency contact mobile is required")]
    [Phone(ErrorMessage = "Invalid phone number")]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string Mobile { get; set; } = string.Empty;

    public EmergencyContactDto ToDto() => new()
    {
        Name = Name,
        Mobile = Mobile
    };

    public static EmergencyContactFormModel FromDto(EmergencyContactDto dto) => new()
    {
        Name = dto.Name ?? string.Empty,
        Mobile = dto.Mobile ?? string.Empty
    };
}