using Microsoft.Playwright;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.E2ETests.Infrastructure.Pages;

/// <summary>
/// Provides deterministic access to rows in the global tours list without scanning paginator pages.
/// It uses the live API tour order to jump directly to the page that should contain a known tour.
/// </summary>
/// <param name="page">The active Playwright page.</param>
/// <param name="navigateTo">Navigation function that resolves relative application routes.</param>
/// <param name="getAllTours">Function that retrieves the current ordered tours list from the API.</param>
internal sealed class ToursListPage(
    IPage page,
    Func<string, Task> navigateTo,
    Func<Task<GetTourDto[]>> getAllTours
)
{
    private const int ItemsPerPage = 10;
    private const int MaxLookupAttempts = 3;

    /// <summary>
    /// Returns the grid row for a known tour after navigating to the page that should contain it.
    /// </summary>
    /// <param name="tourId">The tour identifier to locate.</param>
    /// <returns>The matching tours table row.</returns>
    public async Task<ILocator> GetTourRow(Guid tourId)
    {
        var href = $"/tours/{tourId}";

        for (var attempt = 0; attempt < MaxLookupAttempts; attempt++)
        {
            var allTours = await getAllTours();
            var tourIndex = FindTourIndex(allTours, tourId);

            await navigateTo("/tours");
            Assert.Equal("Tours", await page.TitleAsync());
            await NavigateToPageContaining(tourIndex);

            var row = page.Locator($"table tbody tr:has(a[href='{href}'])");
            if (await row.CountAsync() > 0)
            {
                return row.First;
            }
        }

        throw new InvalidOperationException(
            $"Tour row '{href}' could not be found after {MaxLookupAttempts} deterministic lookup attempt(s).");
    }

    private static int FindTourIndex(GetTourDto[] allTours, Guid tourId)
    {
        for (var index = 0; index < allTours.Length; index++)
        {
            if (allTours[index].Id == tourId)
            {
                return index;
            }
        }

        throw new InvalidOperationException($"Tour '{tourId}' was not found in the API tours list.");
    }

    private async Task NavigateToPageContaining(int tourIndex)
    {
        var targetPageIndex = tourIndex / ItemsPerPage;
        if (targetPageIndex == 0)
        {
            return;
        }

        var nextButton = page.Locator(".paginator button[aria-label='Go to next page']");
        for (var currentPageIndex = 0; currentPageIndex < targetPageIndex; currentPageIndex++)
        {
            var firstTourLink = page.Locator("table tbody tr a[href^='/tours/']").First;
            var previousHref = await firstTourLink.GetAttributeAsync("href");
            Assert.NotNull(previousHref);

            await nextButton.ClickAsync();
            await page.WaitForFunctionAsync(
                "([selector, href]) => { const element = document.querySelector(selector); return element && element.getAttribute('href') !== href; }",
                new object[] { "table tbody tr a[href^='/tours/']", previousHref });
        }
    }
}
