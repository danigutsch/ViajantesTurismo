using Microsoft.AspNetCore.Components;
using ViajantesTurismo.Management.Web.Components.Pages.Documents;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Documents;

/// <summary>Shared render and form-fill steps for the document hold page tests.</summary>
internal static class HoldPageActions
{
    /// <summary>Renders the details page for a document and waits for the heading.</summary>
    public static IRenderedComponent<Details> Render(BunitContext context, Guid documentId)
    {
        var cut = context.Render<Details>(parameters => parameters.Add(page => page.Id, documentId));
        cut.WaitForAssertion(() => cut.Find("h1"));
        return cut;
    }

    /// <summary>Fills the place-hold form with valid synthetic codes and a future review date.</summary>
    public static async Task FillPlaceForm(IRenderedComponent<Details> cut)
    {
        await cut.Find("#hold-basis").ChangeAsync(new ChangeEventArgs { Value = "LegalClaim" });
        await cut.Find("#hold-purpose").ChangeAsync(new ChangeEventArgs { Value = "PURPOSE-9" });
        await cut.Find("#hold-authority").ChangeAsync(new ChangeEventArgs { Value = "AUTH-9" });
        await cut.Find("#hold-review").ChangeAsync(new ChangeEventArgs { Value = "2030-01-02" });
    }
}
