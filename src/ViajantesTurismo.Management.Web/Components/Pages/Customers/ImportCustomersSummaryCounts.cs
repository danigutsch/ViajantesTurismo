namespace ViajantesTurismo.Management.Web.Components.Pages.Customers;

/// <summary>
/// Represents the aggregate outcome counts displayed after an import completes.
/// </summary>
/// <param name="CreatedCount">Customers newly created.</param>
/// <param name="UpdatedCount">Existing customers updated by overwrite or mixed decisions.</param>
/// <param name="SkippedCount">Conflicting rows kept unchanged.</param>
/// <param name="FailedCount">Rows that failed to import.</param>
internal sealed record ImportCustomersSummaryCounts(int CreatedCount, int UpdatedCount, int SkippedCount, int FailedCount);
