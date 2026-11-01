using System.Globalization;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Filters and paging of the cross-document hold overview; every filter is optional.</summary>
/// <param name="State">The disjoint state filter: <c>Active</c>, <c>OverdueReview</c> or <c>Released</c>.</param>
/// <param name="Basis">The hold basis filter.</param>
/// <param name="ReleasedSince">The inclusive lower bound of the release instant.</param>
/// <param name="Page">The one-based page number.</param>
/// <param name="PageSize">The page size.</param>
public sealed record DocumentHoldOverviewQuery(
    string? State = null,
    string? Basis = null,
    DateTimeOffset? ReleasedSince = null,
    int Page = 1,
    int PageSize = 25)
{
    /// <summary>Builds the query string, including the leading question mark.</summary>
    /// <returns>The escaped query string.</returns>
    public string ToQueryString()
    {
        List<string> parts = [];
        AddText(parts, "state", State);
        AddText(parts, "basis", Basis);
        if (ReleasedSince is { } since)
        {
            AddText(parts, "releasedSince", since.ToString("O", CultureInfo.InvariantCulture));
        }

        AddText(parts, "page", Page.ToString(CultureInfo.InvariantCulture));
        AddText(parts, "pageSize", PageSize.ToString(CultureInfo.InvariantCulture));
        return "?" + string.Join('&', parts);
    }

    private static void AddText(List<string> parts, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add($"{name}={Uri.EscapeDataString(value)}");
        }
    }
}
