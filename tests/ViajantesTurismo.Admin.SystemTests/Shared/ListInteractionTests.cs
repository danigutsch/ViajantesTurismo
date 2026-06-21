using System.Globalization;
using System.Text.RegularExpressions;

namespace ViajantesTurismo.Admin.SystemTests.Shared;

/// <summary>
/// E2E smoke tests for list sorting and pagination behavior using owned setup data.
/// </summary>
public class ListInteractionTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    private const string DescendingSortSelector = "th[aria-sort='descending']";

    [Fact]
    public async Task Tour_List_Sort_Smoke_Works_For_Name_Column()
    {
        // Arrange
        var uid = Guid.NewGuid().ToString("N")[..8];
        _ = await ApiClient.CreateTour(new CreateTourOptions
        {
            Identifier = $"SRT-{uid}-A",
            Name = $"Sort Smoke {uid} A",
            StartDate = DateTime.UtcNow.AddDays(10),
            EndDate = DateTime.UtcNow.AddDays(17),
            Price = 500m
        });
        _ = await ApiClient.CreateTour(new CreateTourOptions
        {
            Identifier = $"SRT-{uid}-Z",
            Name = $"Sort Smoke {uid} Z",
            StartDate = DateTime.UtcNow.AddDays(40),
            EndDate = DateTime.UtcNow.AddDays(47),
            Price = 1500m
        });

        // Act
        await NavigateTo("/tours");
        await Expect(Page).ToHaveTitleAsync("Tours");

        var toursTable = Page.Locator("table");
        var tourNameCells = toursTable.Locator("tbody tr td:nth-child(2)");

        // Assert: tours sort by Name ascending / descending.
        await toursTable.GetButton("Name").ClickAsync();
        await Expect(toursTable.Locator("th[aria-sort='ascending']")).ToContainTextAsync("Name");
        await AssertVisibleCellTextsAreSorted(tourNameCells, descending: false);

        await toursTable.GetButton("Name").ClickAsync();
        await Expect(toursTable.Locator(DescendingSortSelector)).ToContainTextAsync("Name");
        await AssertVisibleCellTextsAreSorted(tourNameCells, descending: true);
    }

    [Fact]
    public async Task Customer_List_Paginator_Smoke_Works_And_Preserves_Sort()
    {
        // Arrange
        var uid = Guid.NewGuid().ToString("N")[..8];
        for (var index = 0; index <= 10; index++)
        {
            await ApiClient.CreateCustomer(firstName: $"List{uid}{index:00}", lastName: "Smoke");
        }

        // Act
        await NavigateTo("/customers");
        await Expect(Page).ToHaveTitleAsync("Customers");

        var customersTable = Page.Locator("table");
        await Expect(Page.Locator(".paginator")).ToBeVisibleAsync();
        var paginator = Page.Locator(".paginator");
        var paginationText = paginator.Locator(".pagination-text");

        var firstPage = await ReadPaginationState(paginationText);
        Assert.Equal(1, firstPage.CurrentPage);
        Assert.True(firstPage.TotalPages >= 2, $"Expected at least 2 pages, but found {firstPage.TotalPages}.");

        await customersTable.GetButton("Name").ClickAsync();
        await customersTable.GetButton("Name").ClickAsync();
        await Expect(customersTable.Locator(DescendingSortSelector)).ToContainTextAsync("Name");

        var firstNameCell = customersTable.Locator("tbody tr td:nth-child(1)").First;
        var customerNameCells = customersTable.Locator("tbody tr td:nth-child(1)");
        var firstPageFirstName = await firstNameCell.InnerTextAsync();
        await AssertVisibleCellTextsAreSorted(customerNameCells, descending: true);

        await paginator.Locator("button[aria-label='Go to next page']").ClickAsync();
        await Expect(paginationText).ToContainTextAsync("Page 2 of");
        var secondPage = await ReadPaginationState(paginationText);
        Assert.Equal(2, secondPage.CurrentPage);
        Assert.Equal(firstPage.TotalPages, secondPage.TotalPages);
        Assert.NotEqual(firstPageFirstName, await firstNameCell.InnerTextAsync());
        await AssertVisibleCellTextsAreSorted(customerNameCells, descending: true);
        await Expect(customersTable.Locator(DescendingSortSelector)).ToContainTextAsync("Name");

        await paginator.Locator("button[aria-label='Go to previous page']").ClickAsync();
        await Expect(paginationText).ToContainTextAsync("Page 1 of");
        var returnedPage = await ReadPaginationState(paginationText);
        Assert.Equal(1, returnedPage.CurrentPage);
        Assert.Equal(firstPage.TotalPages, returnedPage.TotalPages);
        Assert.Equal(firstPageFirstName, await firstNameCell.InnerTextAsync());
        await AssertVisibleCellTextsAreSorted(customerNameCells, descending: true);
        await Expect(customersTable.Locator(DescendingSortSelector)).ToContainTextAsync("Name");
    }

    private static async Task AssertVisibleCellTextsAreSorted(ILocator cells, bool descending)
    {
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

    private static async Task<(int CurrentPage, int TotalPages)> ReadPaginationState(ILocator paginationText)
    {
        var text = await paginationText.InnerTextAsync();
        var match = Regex.Match(text, @"Page\s+(?<current>\d+)\s+of\s+(?<total>\d+)");

        Assert.True(match.Success, $"Expected pagination text in 'Page N of M' format, but found '{text}'.");

        return (int.Parse(match.Groups["current"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["total"].Value, CultureInfo.InvariantCulture));
    }
}
