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
    public async Task Finalized_document_shows_only_the_mediated_download_link_and_read_only_fields()
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
        await cut.WaitForAssertionAsync(() => cut.Find("h1"));

        // Assert
        cut.FindAll("input").ShouldBeEmpty();
        cut.FindAll("label").ShouldBeEmpty();
        cut.FindAll("a").ShouldContain(link =>
            link.GetAttribute("href") == $"/documents/{document.Id}/download"
            && link.TextContent.Contains("Download artifact", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Regenerate_navigates_to_the_replacement_revision()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        var original = new GetDocumentDto
        {
            Id = Guid.CreateVersion7(),
            BookingId = bookingId,
            Revision = 1,
            TemplateId = "tour-service-contract",
            TemplateVersion = "1",
            SourceVersion = "SOURCE-VERSION-1",
            Status = DocumentStatusDto.Finalized,
            Fields = [],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            FinalizedAt = DateTime.UtcNow,
            HasFinalizedArtifact = true
        };
        var replacement = original with
        {
            Id = Guid.CreateVersion7(),
            Revision = 2,
            SourceVersion = "SOURCE-VERSION-2",
            Status = DocumentStatusDto.DraftGenerated,
            FinalizedAt = null,
            HasFinalizedArtifact = false
        };
        documentsApiClient.AddDocument(original);
        documentsApiClient.AddDocument(replacement);
        documentsApiClient.RegeneratedDocument = replacement;
        var cut = Render<Details>(parameters => parameters.Add(page => page.Id, original.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("h1"));

        // Act
        await cut.InvokeAsync(() => cut.Find("button").Click());

        // Assert
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.Uri.ShouldBe($"http://localhost/documents/{replacement.Id}");

        // Act
        await cut.InvokeAsync(() => cut.Find("button").Click());

        // Assert
        documentsApiClient.LastBeginReviewDocumentId.ShouldBe(replacement.Id);

        var otherRevision = replacement with { Id = Guid.CreateVersion7() };
        documentsApiClient.AddDocument(otherRevision);
        cut.Render(parameters => parameters.Add(page => page.Id, otherRevision.Id));

        // Act
        await cut.InvokeAsync(() => cut.Find("button").Click());

        // Assert
        documentsApiClient.LastBeginReviewDocumentId.ShouldBe(otherRevision.Id);
    }

    [Fact]
    public async Task Load_failure_shows_a_retryable_error_instead_of_not_found()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        documentsApiClient.GetDocumentException = new HttpRequestException("Service unavailable.");

        // Act
        var cut = Render<Details>(parameters => parameters.Add(page => page.Id, documentId));
        await cut.WaitForAssertionAsync(() => cut.Find("[role='alert']"));

        // Assert
        cut.Find("[role='alert']").TextContent.ShouldContain("The document could not be loaded", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("Document not found.", StringComparison.Ordinal);
        cut.Find("button").TextContent.ShouldContain("Retry", StringComparison.Ordinal);

        documentsApiClient.GetDocumentException = null;
        documentsApiClient.AddDocument(new GetDocumentDto
        {
            Id = documentId,
            BookingId = Guid.CreateVersion7(),
            Revision = 1,
            TemplateId = "tour-service-contract",
            TemplateVersion = "1",
            SourceVersion = "SOURCE-VERSION",
            Status = DocumentStatusDto.DraftGenerated,
            Fields = [],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            HasFinalizedArtifact = false
        });

        // Act
        await cut.InvokeAsync(() => cut.Find("button").Click());

        // Assert
        await cut.WaitForAssertionAsync(() => cut.Find("h1").TextContent.ShouldContain("Document Details", StringComparison.Ordinal));
        cut.FindAll("[role='alert']").ShouldBeEmpty();
    }
}
