using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Management.Web.Models;

/// <summary>
/// Form model for a customer address.
/// </summary>
internal sealed class AddressFormModel
{
    /// <summary>
    /// Street name and number.
    /// </summary>
    [Required(ErrorMessage = "Street is required")]
    [MaxLength(ContractConstants.MaxNameLength)]
    public string Street { get; set; } = string.Empty;

    /// <summary>
    /// Optional address complement, such as apartment or block.
    /// </summary>
    [MaxLength(ContractConstants.MaxNameLength)]
    public string? Complement
    {
        get; set;
    }

    /// <summary>
    /// Neighborhood or district.
    /// </summary>
    [Required(ErrorMessage = "Neighborhood is required")]
    [MaxLength(ContractConstants.MaxNameLength)]
    public string Neighborhood { get; set; } = string.Empty;

    /// <summary>
    /// Postal code.
    /// </summary>
    [Required(ErrorMessage = "Postal code is required")]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// City name.
    /// </summary>
    [Required(ErrorMessage = "City is required")]
    [MaxLength(ContractConstants.MaxNameLength)]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State or province.
    /// </summary>
    [Required(ErrorMessage = "State is required")]
    [MaxLength(ContractConstants.MaxNameLength)]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Country name.
    /// </summary>
    [Required(ErrorMessage = "Country is required")]
    [MaxLength(ContractConstants.MaxNameLength)]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Converts the validated form to an address DTO.
    /// </summary>
    public AddressDto ToDto() => new()
    {
        Street = Street,
        Complement = Complement,
        Neighborhood = Neighborhood,
        PostalCode = PostalCode,
        City = City,
        State = State,
        Country = Country
    };

    /// <summary>
    /// Creates a form model from an address DTO.
    /// </summary>
    public static AddressFormModel FromDto(AddressDto dto) => new()
    {
        Street = dto.Street,
        Complement = dto.Complement,
        Neighborhood = dto.Neighborhood ?? string.Empty,
        PostalCode = dto.PostalCode,
        City = dto.City,
        State = dto.State,
        Country = dto.Country
    };
}
