namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>A bounded slice of customer choices.</summary>
/// <param name="Items">The choices in stable display order.</param>
/// <param name="Total">The number of choices that match the search.</param>
public sealed record GetCustomerOptionsDto(IReadOnlyList<GetCustomerOptionDto> Items, int Total);
