using Microsoft.AspNetCore.Components;
using PandaBear.Blazor;
using PandaBear.HttpClients;
using PandaBear.Testing;
using ViajantesTurismo.Management.Web.Components;
using ViajantesTurismo.Management.Web.Components.Pages.Documents;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Documents;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[TestScope(TestScope.Component)]
public sealed class HoldsPageTests : BunitContext
{
    private readonly FakeDocumentsApiClient documentsApiClient = new();

    public HoldsPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddUiFailureReporting();
        var authorization = AddAuthorization();
        authorization.SetAuthorized("admin@example.test", AuthorizationState.Authorized);
        authorization.SetRoles("Admin");
        Services.AddSingleton<IDocumentsApiClient>(documentsApiClient);
        SetRendererInfo(new RendererInfo("Server", true));
    }

    [Fact]
    public void Declares_the_holds_overview_route_for_admins_only()
    {
        // Act
        var route = typeof(Holds).GetCustomAttributes(typeof(RouteAttribute), false).OfType<RouteAttribute>().ShouldHaveSingleItem();
        var authorize = typeof(Holds).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().ShouldHaveSingleItem();

        // Assert
        route.Template.ShouldBe("/documents/holds");
        authorize.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void Router_does_not_render_the_holds_overview_for_an_operator()
    {
        // Arrange
        using var context = new BunitContext();
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("operator@example.test", AuthorizationState.Authorized);
        authorization.SetRoles("Operator");
        context.Services.AddSingleton<IDocumentsApiClient>(documentsApiClient);
        context.Services.AddUiFailureReporting();
        context.SetRendererInfo(new RendererInfo("Server", true));
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/documents/holds");

        // Act
        var cut = context.Render<Routes>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindComponents<Holds>().ShouldBeEmpty();
            cut.Markup.ShouldContain("Not authorized", StringComparison.Ordinal);
        });
        documentsApiClient.OverviewQueries.ShouldBeEmpty();
    }

    [Fact]
    public void Opens_on_the_active_view_and_lists_each_hold_with_a_text_status()
    {
        // Arrange
        var hold = DocumentHoldTestData.Create(basis: "RegulatoryInquiry");
        var lineageId = Guid.CreateVersion7();
        documentsApiClient.OverviewHandler = _ => DocumentHoldTestData.OverviewPage([new GetDocumentHoldOverviewItemDto { LineageId = lineageId, Hold = hold }], hasMore: false);

        // Act
        var cut = Render<Holds>();

        // Assert
        var query = documentsApiClient.OverviewQueries.ShouldHaveSingleItem();
        query.State.ShouldBe("Active");
        query.Page.ShouldBe(1);
        var row = cut.Find("tbody tr");
        row.TextContent.ShouldContain(lineageId.ToString(), StringComparison.Ordinal);
        row.TextContent.ShouldContain("RegulatoryInquiry", StringComparison.Ordinal);
        row.QuerySelector(".badge")!.TextContent.ShouldContain("Active", StringComparison.Ordinal);
        cut.Find("button[aria-pressed='true']").TextContent.ShouldBe("Active");
        cut.Find("caption").TextContent.ShouldContain("Active holds", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Overdue review", "OverdueReview", false)]
    [InlineData("Recently released", "Released", true)]
    public async Task Switching_view_queries_the_matching_disjoint_state(string label, string state, bool sendsReleasedSince)
    {
        // Arrange
        var cut = Render<Holds>();

        // Act
        await cut.FindAll("button[aria-pressed]").ShouldHaveSingleItem(button => button.TextContent == label).ClickAsync();

        // Assert
        var query = documentsApiClient.OverviewQueries.Last();
        query.State.ShouldBe(state);
        query.ReleasedSince.HasValue.ShouldBe(sendsReleasedSince);
        cut.Find("button[aria-pressed='true']").TextContent.ShouldBe(label);
    }

    [Fact]
    public async Task Choosing_a_basis_filters_the_query_and_returns_to_page_one()
    {
        // Arrange
        documentsApiClient.OverviewHandler = _ => DocumentHoldTestData.OverviewPage([new GetDocumentHoldOverviewItemDto { LineageId = Guid.CreateVersion7(), Hold = DocumentHoldTestData.Create() }], hasMore: true);
        var cut = Render<Holds>();
        await cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Next page").ClickAsync();

        // Act
        await cut.Find("#holds-basis-filter").ChangeAsync(new ChangeEventArgs { Value = "Investigation" });

        // Assert
        var query = documentsApiClient.OverviewQueries.Last();
        query.Basis.ShouldBe("Investigation");
        query.Page.ShouldBe(1);
        cut.Find("label[for='holds-basis-filter']").TextContent.ShouldBe("Basis");
    }

    [Fact]
    public async Task Paging_follows_has_more_and_the_first_page_cannot_go_back()
    {
        // Arrange
        documentsApiClient.OverviewHandler = query => DocumentHoldTestData.OverviewPage([new GetDocumentHoldOverviewItemDto { LineageId = Guid.CreateVersion7(), Hold = DocumentHoldTestData.Create() }], hasMore: query.Page < 2);
        var cut = Render<Holds>();
        var previous = cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Previous page");
        previous.HasAttribute("disabled").ShouldBeTrue();

        // Act
        await cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Next page").ClickAsync();

        // Assert
        documentsApiClient.OverviewQueries.Last().Page.ShouldBe(2);
        cut.Find("nav[aria-label='Holds pages']").TextContent.ShouldContain("Page 2", StringComparison.Ordinal);
        cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Next page").HasAttribute("disabled").ShouldBeTrue();
        cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Previous page").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void An_empty_view_says_so()
    {
        // Act
        var cut = Render<Holds>();

        // Assert
        cut.Find("[role='status']").TextContent.ShouldContain("No holds match", StringComparison.Ordinal);
    }

    [Fact]
    public void A_failed_load_shows_a_visible_error_with_retry()
    {
        // Arrange
        documentsApiClient.OverviewFailure = HttpOutcomeKind.Forbidden;

        // Act
        var cut = Render<Holds>();

        // Assert
        cut.Find("[role='alert']").TextContent.Trim().ShouldNotBeEmpty();
        cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Retry");
    }
}
