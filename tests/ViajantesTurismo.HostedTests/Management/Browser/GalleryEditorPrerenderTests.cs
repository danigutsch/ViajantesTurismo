using System.Net;
using PandaBear.Testing;
using PandaBear.Testing.AspNetCore;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Verifies the first render of the actual Management gallery editor route.</summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.System)]
public sealed class GalleryEditorPrerenderTests
{
    [Fact]
    public async Task Large_album_prerender_hydrates_one_page_and_one_explicit_cover()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var catalog = new GalleryEditorCatalogStub(longLabels: true);
        await using var host = await GalleryEditorBrowserScenario.Start(catalog, ct);
        using var client = new HttpClient { BaseAddress = host.BaseUri };

        // Act
        using var response = await client.GetAsync(new Uri(catalog.EditorPage, UriKind.Relative), ct);
        var html = await response.Content.ReadAsStringAsync(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("<h1>Edit gallery</h1>", StringComparison.Ordinal);
        html.ShouldContain("Photo 024", StringComparison.Ordinal);
        html.ShouldNotContain("Photo 025", StringComparison.Ordinal);
        html.ShouldContain("Photo 500", StringComparison.Ordinal);
        html.ShouldNotContain("Photo 803", StringComparison.Ordinal);
        html.ShouldContain("Showing 1 to 24 of 500 chosen images.", StringComparison.Ordinal);
        html.ShouldNotContain("id=\"gallery-photos-panel\"", StringComparison.Ordinal);
        var stateSize = PrerenderedComponentState.ServerStateSize(html);
        stateSize.ShouldBeGreaterThan(0);
        PrerenderedComponentState.ShouldKeepServerStateBelow(html, 28_000);
        html.Split("src=\"/catalog/media/images/", StringSplitOptions.None).Length.ShouldBe(26);
        catalog.ChoiceRequests.Select(request => request.IncludedPhotoIds.ShouldNotBeNull().Count).ShouldBe([24, 1]);
    }
}
