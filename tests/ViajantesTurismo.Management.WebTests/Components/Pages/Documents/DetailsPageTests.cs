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
        var authorization = AddAuthorization();
        authorization.SetAuthorized("admin@example.test", AuthorizationState.Authorized);
        authorization.SetRoles("Admin");
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
        var authorization = staticContext.AddAuthorization();
        authorization.SetAuthorized("admin@example.test", AuthorizationState.Authorized);
        authorization.SetRoles("Admin");
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
    public void Router_does_not_render_document_details_for_an_operator()
    {
        // Arrange
        using var context = new BunitContext();
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("operator@example.test", AuthorizationState.Authorized);
        authorization.SetRoles("Operator");
        context.Services.AddSingleton<IDocumentsApiClient>(new FakeDocumentsApiClient());
        context.SetRendererInfo(new RendererInfo("Server", true));
        var documentId = Guid.CreateVersion7();
        var navigationManager = context.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo($"/documents/{documentId}");

        // Act
        var cut = context.Render<Routes>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindComponents<Details>().ShouldBeEmpty();
            cut.Markup.ShouldContain("Not authorized", StringComparison.Ordinal);
        });
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
            && link.HasAttribute("download")
            && link.GetAttribute("data-enhance-nav") == "false"
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
    public async Task Editable_fields_have_distinct_save_button_accessible_names()
    {
        // Arrange
        var document = new GetDocumentDto
        {
            Id = Guid.CreateVersion7(),
            BookingId = Guid.CreateVersion7(),
            Revision = 1,
            TemplateId = "tour-service-contract",
            TemplateVersion = "1",
            SourceVersion = "SOURCE-VERSION",
            Status = DocumentStatusDto.DraftGenerated,
            Fields =
            [
                new GetDocumentFieldDto
                {
                    FieldId = "greeting",
                    Label = "Greeting",
                    RenderedValue = "Dear customer",
                    IsEditable = true
                },
                new GetDocumentFieldDto
                {
                    FieldId = "trip-note",
                    Label = "Trip note",
                    RenderedValue = "Bring a hat",
                    IsEditable = true
                }
            ],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            HasFinalizedArtifact = false
        };
        documentsApiClient.AddDocument(document);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(page => page.Id, document.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("h1"));

        // Assert
        cut.Find("button[aria-label='Save Greeting']").ShouldNotBeNull();
        cut.Find("button[aria-label='Save Trip note']").ShouldNotBeNull();
    }

    [Theory]
    [InlineData(DocumentStatusDto.DraftGenerated, "Start review", true)]
    [InlineData(DocumentStatusDto.InReview, "Request changes|Approve", true)]
    [InlineData(DocumentStatusDto.ChangesRequested, "Start review", true)]
    [InlineData(DocumentStatusDto.Approved, "Request changes|Finalize", true)]
    [InlineData(DocumentStatusDto.Finalized, "Download artifact|Regenerate|Void", false)]
    [InlineData(DocumentStatusDto.Superseded, "", false)]
    [InlineData(DocumentStatusDto.Voided, "", false)]
    public async Task Actions_and_editability_match_document_status(
        DocumentStatusDto status,
        string expectedActions,
        bool expectsEditableField)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(expectedActions);
        var document = DocumentDetailsTestData.Create(
            status,
            hasFinalizedArtifact: status == DocumentStatusDto.Finalized);
        documentsApiClient.AddDocument(document);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(page => page.Id, document.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("h1"));

        // Assert
        var expectedActionNames = expectedActions.Split(
            '|',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var actualActionNames = cut
            .FindAll("[aria-label='Document actions'] button, [aria-label='Document actions'] a")
            .Select(action => action.TextContent.Trim())
            .ToArray();
        actualActionNames.ShouldBeEquivalentTo(expectedActionNames);
        cut.FindAll("input[id^='document-field-']").Any().ShouldBe(expectsEditableField);
    }

    [Fact]
    public async Task Saving_an_editable_field_sends_the_value_and_refreshes_the_document()
    {
        // Arrange
        const string reviewedValue = "Reviewed customer greeting";
        var original = DocumentDetailsTestData.Create(DocumentStatusDto.DraftGenerated);
        var updated = original with
        {
            Status = DocumentStatusDto.InReview,
            Fields = [original.Fields[0] with { RenderedValue = reviewedValue }]
        };
        documentsApiClient.AddDocument(original);
        documentsApiClient.UpdatedDocument = updated;
        var cut = Render<Details>(parameters => parameters.Add(page => page.Id, original.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("#document-field-greeting"));

        // Act
        await cut.InvokeAsync(() => cut.Find("#document-field-greeting").Change(reviewedValue));
        await cut.InvokeAsync(() => cut.Find("button[aria-label='Save Greeting']").Click());

        // Assert
        documentsApiClient.LastUpdatedDocumentId.ShouldBe(original.Id);
        documentsApiClient.LastUpdatedFieldId.ShouldBe("greeting");
        documentsApiClient.LastUpdatedFieldValue.ShouldBe(reviewedValue);
        cut.FindAll("dd")[0].TextContent.Trim().ShouldBe("InReview");
        cut.Find("#document-field-greeting").GetAttribute("value").ShouldBe(reviewedValue);
    }

    [Fact]
    public async Task Finalized_document_without_an_artifact_hides_the_download_link()
    {
        // Arrange
        var document = DocumentDetailsTestData.Create(
            DocumentStatusDto.Finalized,
            hasFinalizedArtifact: false,
            fields: []);
        documentsApiClient.AddDocument(document);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(page => page.Id, document.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("h1"));

        // Assert
        cut.FindAll($"a[href='/documents/{document.Id}/download']").ShouldBeEmpty();
        cut.FindAll("button").ShouldContain(button =>
            button.TextContent.Contains("Regenerate", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Document_actions_are_exposed_as_a_named_group()
    {
        // Arrange
        var document = DocumentDetailsTestData.Create(DocumentStatusDto.DraftGenerated, fields: []);
        documentsApiClient.AddDocument(document);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(page => page.Id, document.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("h1"));

        // Assert
        cut.Find("[role='group'][aria-label='Document actions']").ShouldNotBeNull();
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
