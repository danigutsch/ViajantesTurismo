using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Admin.Web.Models;

internal sealed class AddressFormModel
{
    [Required(ErrorMessage = "Street is required")]
    [MaxLength(ContractConstants.MaxNameLength)]
    public string Street { get; set; } = string.Empty;

    [MaxLength(ContractConstants.MaxNameLength)]
    public string? Complement { get; set; }

    [Required(ErrorMessage = "Neighborhood is required")]
    [MaxLength(ContractConstants.MaxNameLength)]
    public string Neighborhood { get; set; } = string.Empty;

    [Required(ErrorMessage = "Postal code is required")]
    [MaxLength(ContractConstants.MaxDefaultLength)]
    public string PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required")]
    [MaxLength(ContractConstants.MaxNameLength)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "State is required")]
    [MaxLength(ContractConstants.MaxNameLength)]
    public string State { get; set; } = string.Empty;

    [Required(ErrorMessage = "Country is required")]
    [MaxLength(ContractConstants.MaxNameLength)]
    public string Country { get; set; } = string.Empty;

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