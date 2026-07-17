using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using SharedKernel.Testing;
using ViajantesTurismo.Management.Web.Components;
using ViajantesTurismo.Management.Web.Components.Pages.Documents;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Documents;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[Trait(SharedKernelTestTraitNames.ScopeName, TestTraits.ComponentScope)]
public sealed class DetailsPageTests : BunitContext
{
    private readonly FakeDocumentsApiClient documentsApiClient = new();

    public DetailsPageTests()
    {
        Services.AddSingleton<IDocumentsApiClient>(documentsApiClient);
        SetRendererInfo(new RendererInfo("Server", true));
    }

    [Fact]
    public void Declares_the_document_details_route()
    {
        // Act
        var route = typeof(Details).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .ShouldHaveSingleItem();

        // Assert
        route.Template.ShouldBe("/documents/{Id:guid}");
    }

    [Fact]
    public void Router_matches_the_document_details_route()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo($"/documents/{documentId}");

        // Act
        var cut = Render<Routes>();

        // Assert
        var router = cut.FindComponent<Router>();
        router.Instance.AppAssembly.ShouldBe(typeof(Details).Assembly);
        var details = cut.FindComponent<Details>();
        details.Instance.Id.ShouldBe(documentId);
    }

    [Fact]
    public void Static_router_matches_the_document_details_route()
    {
        // Arrange
        using var staticContext = new BunitContext();
        staticContext.Services.AddSingleton<IDocumentsApiClient>(new FakeDocumentsApiClient());
        staticContext.SetRendererInfo(new RendererInfo("Static", false));
        var documentId = Guid.CreateVersion7();
        var navigationManager = staticContext.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo($"/documents/{documentId}");

        // Act
        var cut = staticContext.Render<Routes>();

        // Assert
        var details = cut.FindComponent<Details>();
        details.Instance.Id.ShouldBe(documentId);
    }

    [Fact]
    public void Finalized_document_shows_only_the_mediated_download_link_and_read_only_fields()
    {
        // Arrange
        var document = new GetDocumentDto
        {
            Id = Guid.CreateVersion7(),
            BookingId = Guid.CreateVersion7(),
            Revision = 2,
            TemplateId = "tour-service-contract",
            TemplateVersion = "1",
            SourceVersion = "SOURCE-VERSION",
            Status = DocumentStatusDto.Finalized,
            Fields =
            [
                new GetDocumentFieldDto
                {
                    FieldId = "greeting",
                    Label = "Greeting",
                    RenderedValue = "Dear customer",
                    IsEditable = true
                }
            ],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            FinalizedAt = DateTime.UtcNow,
            HasFinalizedArtifact = true
        };
        documentsApiClient.AddDocument(document);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(page => page.Id, document.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));

        // Assert
        cut.FindAll("input").ShouldBeEmpty();
        cut.FindAll("a").ShouldContain(link =>
            link.GetAttribute("href") == $"/documents/{document.Id}/download"
            && link.TextContent.Contains("Download artifact", StringComparison.Ordinal));
    }
}
