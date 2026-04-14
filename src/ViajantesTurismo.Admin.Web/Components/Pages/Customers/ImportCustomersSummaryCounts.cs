namespace ViajantesTurismo.Admin.Web.Components.Pages.Customers;

/// <summary>
/// Represents the aggregate outcome counts displayed after an import completes.
/// </summary>
internal sealed record ImportCustomersSummaryCounts(int CreatedCount, int UpdatedCount, int SkippedCount, int FailedCount);
