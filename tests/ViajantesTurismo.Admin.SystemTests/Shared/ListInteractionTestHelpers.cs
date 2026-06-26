using System.Globalization;
using System.Text.RegularExpressions;

namespace ViajantesTurismo.Admin.SystemTests.Shared;

public static class ListInteractionTestHelpers
{
    public static async Task<(int CurrentPage, int TotalPages)> ReadPaginationState(ILocator paginationText)
    {
        ArgumentNullException.ThrowIfNull(paginationText);

        var text = await paginationText.InnerTextAsync();
        var match = Regex.Match(text, @"Page\s+(?<current>\d+)\s+of\s+(?<total>\d+)");

        Assert.True(match.Success, $"Expected pagination text in 'Page N of M' format, but found '{text}'.");

        return (int.Parse(match.Groups["current"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["total"].Value, CultureInfo.InvariantCulture));
    }

    public static async Task AssertVisibleCellTextsAreSorted(ILocator cells, bool descending)
    {
        ArgumentNullException.ThrowIfNull(cells);

        var visibleTexts = (await cells.AllInnerTextsAsync())
            .Select(text => text.Trim())
            .Where(text => text.Length > 0)
            .ToArray();

        Assert.True(visibleTexts.Length > 1, "Expected at least two visible rows to verify sorting.");

        var sortedTexts = descending
            ? visibleTexts.OrderByDescending(text => text, StringComparer.OrdinalIgnoreCase).ToArray()
            : visibleTexts.OrderBy(text => text, StringComparer.OrdinalIgnoreCase).ToArray();

        Assert.Equal(sortedTexts, visibleTexts);
    }
}
