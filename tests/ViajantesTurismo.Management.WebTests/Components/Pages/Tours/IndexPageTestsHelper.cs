using Index = ViajantesTurismo.Management.Web.Components.Pages.Tours.Index;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Tours;

/// <summary>Assertion helpers for the tour index page tests.</summary>
internal static class IndexPageTestsHelper
{
    /// <summary>Asserts the Name column header is the one marked as ascending.</summary>
    internal static void AssertNameSortIsAscending(IRenderedComponent<Index> cut)
    {
        ArgumentNullException.ThrowIfNull(cut);

        cut.WaitForAssertion(() =>
        {
            var header = cut.Find("th[aria-sort='ascending']");
            header.TextContent.Trim().ShouldBe("Name");
        });
    }
}
