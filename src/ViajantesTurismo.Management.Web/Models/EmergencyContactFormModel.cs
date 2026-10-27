using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Management.Web.Models;

/// <summary>
/// Form model for a customer emergency contact.
/// </summary>
internal sealed class EmergencyContactFormModel
{
    /// <summary>
    /// Full name of the emergency contact.
    /// </summary>
    [Required(ErrorMessage = "Emergency contact name is required")]
    [MaxLength(ContractConstants.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mobile phone number of the emergency contact.
    /// </summary>
    [Required(ErrorMessage = "Emergency contact mobile is required")]
    [Phone(ErrorMessage = "Invalid phone number")]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string Mobile { get; set; } = string.Empty;

    /// <summary>
    /// Converts the validated form to an emergency contact DTO.
    /// </summary>
    public EmergencyContactDto ToDto() => new()
    {
        Name = Name,
        Mobile = Mobile
    };

    /// <summary>
    /// Creates a form model from an emergency contact DTO.
    /// </summary>
    public static EmergencyContactFormModel FromDto(EmergencyContactDto dto) => new()
    {
        Name = dto.Name,
        Mobile = dto.Mobile
    };
}
