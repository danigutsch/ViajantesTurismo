using Index = ViajantesTurismo.Management.Web.Components.Pages.Tours.Index;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Tours;

internal static class IndexPageTestsHelper
{
    internal static void AssertNameSortIsAscending(IRenderedComponent<Index> cut)
    {
        ArgumentNullException.ThrowIfNull(cut);

        cut.WaitForAssertion(() =>
        {
            var header = cut.Find("th[aria-sort='ascending']");
            (header.TextContent.Trim()).ShouldBe("Name");
        });
    }
}
