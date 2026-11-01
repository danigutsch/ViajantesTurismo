using AngleSharp.Dom;
using ViajantesTurismo.Management.Web.Components.Pages.Documents;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Documents;

/// <summary>Element lookups for the manual review controls of the document details page.</summary>
internal static class ManualReviewPageActions
{
    /// <summary>Finds the single Approve action button.</summary>
    public static IElement ApproveButton(IRenderedComponent<Details> cut) =>
        cut.FindAll("[aria-label='Document actions'] button")
            .ShouldHaveSingleItem(button => button.TextContent.Trim() == "Approve");

    /// <summary>Finds the single Acknowledge changes button.</summary>
    public static IElement AcknowledgeButton(IRenderedComponent<Details> cut) =>
        cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Trim() == "Acknowledge changes");
}
