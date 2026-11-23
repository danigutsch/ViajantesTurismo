namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Contains a bounded page of customer summaries and the total count.</summary>
/// <param name="Items">The rows selected for this page.</param>
/// <param name="Total">The total number of customers visible to the request.</param>
public sealed record GetCustomerPageDto(IReadOnlyList<GetCustomerListItemDto> Items, int Total);
