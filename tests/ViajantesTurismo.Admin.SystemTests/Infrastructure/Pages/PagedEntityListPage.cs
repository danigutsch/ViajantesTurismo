namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Pages;

/// <summary>
/// Locates a known entity in a paginated global list without scanning pages.
/// </summary>
/// <typeparam name="TEntity">The API entity type that supplies the list ordering.</typeparam>
/// <param name="page">The active Playwright page.</param>
/// <param name="navigateTo">Navigation function that resolves relative application routes.</param>
/// <param name="getAllEntities">Function that retrieves the ordered entities from the API.</param>
/// <param name="getId">Function that returns an entity identifier.</param>
/// <param name="listPath">The list route.</param>
/// <param name="detailsPath">The details-route prefix.</param>
/// <param name="expectedTitle">The expected list-page title.</param>
internal sealed class PagedEntityListPage<TEntity>(
    IPage page,
    Func<string, Task> navigateTo,
    Func<Task<TEntity[]>> getAllEntities,
    Func<TEntity, Guid> getId,
    string listPath,
    string detailsPath,
    string expectedTitle)
{
    private const int ItemsPerPage = 10;
    private const int MaxLookupAttempts = 3;

    /// <summary>
    /// Returns the grid row for a known entity after navigating directly to the page that should contain it.
    /// </summary>
    /// <param name="entityId">The entity identifier to locate.</param>
    /// <returns>The matching table row.</returns>
    public async Task<ILocator> GetRow(Guid entityId)
    {
        var href = $"{detailsPath}/{entityId}";

        for (var attempt = 0; attempt < MaxLookupAttempts; attempt++)
        {
            var allEntities = await getAllEntities();
            var entityIndex = FindEntityIndex(allEntities, entityId);

            await navigateTo(listPath);
            await page.ShouldHaveTitle(expectedTitle);

            var firstEntityLink = page.Locator($"table tbody tr a[href^='{detailsPath}/']").First;
            await firstEntityLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

            await NavigateToPageContaining(entityIndex);

            var row = page.Locator($"table tbody tr:has(a[href='{href}'])");
            if (await row.CountAsync() > 0)
            {
                return row.First;
            }
        }

        throw new InvalidOperationException(
            $"Entity row '{href}' could not be found after {MaxLookupAttempts} deterministic lookup attempt(s).");
    }

    private int FindEntityIndex(TEntity[] allEntities, Guid entityId)
    {
        for (var index = 0; index < allEntities.Length; index++)
        {
            if (getId(allEntities[index]) == entityId)
            {
                return index;
            }
        }

        throw new InvalidOperationException($"Entity '{entityId}' was not found in the API list.");
    }

    private async Task NavigateToPageContaining(int entityIndex)
    {
        var targetPageIndex = entityIndex / ItemsPerPage;
        if (targetPageIndex == 0)
        {
            return;
        }

        var nextButton = page.Locator(".paginator button[aria-label='Go to next page']");
        for (var currentPageIndex = 0; currentPageIndex < targetPageIndex; currentPageIndex++)
        {
            var firstEntityLink = page.Locator($"table tbody tr a[href^='{detailsPath}/']").First;
            var previousHref = (await firstEntityLink.GetAttributeAsync("href")).ShouldNotBeNull();

            await nextButton.ClickAsync();
            await page.WaitForFunctionAsync(
                "([selector, href]) => { const element = document.querySelector(selector); return element && element.getAttribute('href') !== href; }",
                new object[] { $"table tbody tr a[href^='{detailsPath}/']", previousHref });
        }
    }
}
