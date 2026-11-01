using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PandaBear.Blazor;
using PandaBear.HttpClients;
using PandaBear.Testing;
using ViajantesTurismo.Management.Web.Components.Pages.Documents;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Documents;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[TestScope(TestScope.Component)]
public sealed class DetailsPageHoldTests : BunitContext
{
    private readonly FakeDocumentsApiClient documentsApiClient = new();
    private readonly GetDocumentDto document = DocumentDetailsTestData.Create(DocumentStatusDto.Finalized, fields: []);

    public DetailsPageHoldTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddUiFailureReporting();
        var authorization = AddAuthorization();
        authorization.SetAuthorized("admin@example.test", AuthorizationState.Authorized);
        authorization.SetRoles("Admin");
        Services.AddSingleton<IDocumentsApiClient>(documentsApiClient);
        SetRendererInfo(new RendererInfo("Server", true));
        documentsApiClient.AddDocument(document);
    }

    [Fact]
    public void A_restraining_hold_shows_state_basis_review_date_and_the_disposition_notice()
    {
        // Arrange
        var hold = DocumentHoldTestData.Create(basis: "Investigation", reviewDueAt: DateTimeOffset.Parse("2030-03-04T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        documentsApiClient.Holds.Add(hold);

        // Act
        var cut = HoldPageActions.Render(this, document.Id);

        // Assert
        var row = cut.Find($"tr[data-hold-id='{hold.Id}']");
        row.TextContent.ShouldContain("Active", StringComparison.Ordinal);
        row.TextContent.ShouldContain("Investigation", StringComparison.Ordinal);
        row.TextContent.ShouldContain("2030-03-04", StringComparison.Ordinal);
        var notice = cut.Find(".alert-warning");
        notice.TextContent.ShouldContain("Deletion", StringComparison.Ordinal);
        notice.TextContent.ShouldContain("voiding", StringComparison.Ordinal);
        notice.TextContent.ShouldContain("anonymization", StringComparison.Ordinal);
        notice.GetAttribute("role").ShouldBe("status");
    }

    [Fact]
    public void An_overdue_review_is_labelled_with_text_and_not_only_colour()
    {
        // Arrange
        documentsApiClient.Holds.Add(DocumentHoldTestData.Create(reviewDueAt: DateTimeOffset.Parse("2020-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture)));

        // Act
        var cut = HoldPageActions.Render(this, document.Id);

        // Assert
        cut.Find(".badge").TextContent.ShouldContain("review overdue", StringComparison.Ordinal);
        cut.Find(".badge i").GetAttribute("aria-hidden").ShouldBe("true");
    }

    [Fact]
    public void A_released_hold_shows_no_notice_and_no_release_button()
    {
        // Arrange
        documentsApiClient.Holds.Add(DocumentHoldTestData.Create(released: true));

        // Act
        var cut = HoldPageActions.Render(this, document.Id);

        // Assert
        cut.FindAll(".alert-warning").ShouldBeEmpty();
        cut.Find("tbody").TextContent.ShouldContain("Released", StringComparison.Ordinal);
        cut.Find("tbody").TextContent.ShouldContain("CLAIM-CLOSED", StringComparison.Ordinal);
        cut.FindAll("button[aria-label^='Release hold']").ShouldBeEmpty();
    }

    [Fact]
    public void A_document_without_holds_says_so_and_shows_no_notice()
    {
        // Act
        var cut = HoldPageActions.Render(this, document.Id);

        // Assert
        cut.Markup.ShouldContain("No holds have been placed", StringComparison.Ordinal);
        cut.FindAll(".alert-warning").ShouldBeEmpty();
    }

    [Fact]
    public void Hold_section_is_hidden_from_a_user_without_the_admin_role()
    {
        // Arrange
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("operator@example.test", AuthorizationState.Authorized);
        authorization.SetRoles("Operator");
        context.Services.AddUiFailureReporting();
        context.Services.AddSingleton<IDocumentsApiClient>(documentsApiClient);
        context.SetRendererInfo(new RendererInfo("Server", true));
        documentsApiClient.Holds.Add(DocumentHoldTestData.Create());

        // Act
        var cut = context.Render<Details>(parameters => parameters.Add(page => page.Id, document.Id));

        // Assert
        cut.WaitForAssertion(() => cut.Find("h1"));
        cut.Markup.ShouldNotContain("Legal holds", StringComparison.Ordinal);
        cut.FindAll("[data-hold-id]").ShouldBeEmpty();
    }

    [Fact]
    public void A_failed_hold_load_shows_a_visible_error()
    {
        // Arrange
        documentsApiClient.ListHoldsFailure = HttpOutcomeKind.Forbidden;

        // Act
        var cut = HoldPageActions.Render(this, document.Id);

        // Assert
        cut.Find("#document-holds-title").ShouldNotBeNull();
        cut.Find("[role='alert']").TextContent.Trim().ShouldNotBeEmpty();
        cut.Markup.ShouldContain("Holds are not available", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Placing_with_invalid_codes_shows_linked_field_errors_and_calls_nothing()
    {
        // Arrange
        var cut = HoldPageActions.Render(this, document.Id);
        await cut.Find("#hold-purpose").ChangeAsync(new ChangeEventArgs { Value = "jane doe@example.test" });

        // Act
        await cut.Find("form[aria-labelledby='place-hold-title']").SubmitAsync();

        // Assert
        documentsApiClient.PlacedHolds.ShouldBeEmpty();
        cut.Find("#hold-basis").GetAttribute("aria-invalid").ShouldBe("true");
        cut.Find("#hold-basis").GetAttribute("aria-describedby").ShouldBe("hold-basis-error");
        cut.Find("#hold-basis-error").TextContent.ShouldContain("required", StringComparison.Ordinal);
        cut.Find("#hold-purpose").GetAttribute("aria-describedby").ShouldBe("hold-purpose-error");
        cut.Find("#hold-purpose-error").TextContent.ShouldContain("only letters, digits", StringComparison.Ordinal);
        cut.Find("#hold-authority-error").TextContent.ShouldContain("required", StringComparison.Ordinal);
        cut.Find("#hold-review-error").TextContent.ShouldContain("required", StringComparison.Ordinal);
        cut.Find("form [role='alert']").TextContent.ShouldContain("need attention", StringComparison.Ordinal);
        cut.FindAll("label").Select(label => label.GetAttribute("for")).ShouldContain("hold-purpose");
    }

    [Fact]
    public async Task Placing_a_hold_needs_an_explicit_confirmation_and_then_shows_the_audit_outcome()
    {
        // Arrange
        var cut = HoldPageActions.Render(this, document.Id);
        await HoldPageActions.FillPlaceForm(cut);

        // Act
        await cut.Find("form[aria-labelledby='place-hold-title']").SubmitAsync();

        // Assert
        documentsApiClient.PlacedHolds.ShouldBeEmpty();
        var confirmation = cut.Find("section[aria-labelledby='place-hold-confirm-title']");
        confirmation.TextContent.ShouldContain(document.Id.ToString(), StringComparison.Ordinal);
        confirmation.TextContent.ShouldContain("LegalClaim", StringComparison.Ordinal);
        confirmation.TextContent.ShouldContain("PURPOSE-9", StringComparison.Ordinal);
        confirmation.TextContent.ShouldContain("AUTH-9", StringComparison.Ordinal);
        JSInterop.VerifyFocusAsyncInvoke();

        // Act
        await cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Contains("Confirm placement", StringComparison.Ordinal)).ClickAsync();

        // Assert
        var (documentId, dto) = documentsApiClient.PlacedHolds.ShouldHaveSingleItem();
        documentId.ShouldBe(document.Id);
        dto.Basis.ShouldBe("LegalClaim");
        dto.PurposeCode.ShouldBe("PURPOSE-9");
        dto.AuthorityReference.ShouldBe("AUTH-9");
        dto.ReviewDueAt.ShouldBe(DateTimeOffset.Parse("2030-01-02T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        dto.ExpiresAt.ShouldBeNull();
        var status = cut.Find(".alert-success");
        status.GetAttribute("role").ShouldBe("status");
        status.TextContent.ShouldContain("placed by actor-place", StringComparison.Ordinal);
        status.TextContent.ShouldContain("audit trail", StringComparison.Ordinal);
        cut.FindAll("tbody tr").ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Going_back_from_the_placement_confirmation_places_nothing()
    {
        // Arrange
        var cut = HoldPageActions.Render(this, document.Id);
        await HoldPageActions.FillPlaceForm(cut);
        await cut.Find("form[aria-labelledby='place-hold-title']").SubmitAsync();

        // Act
        await cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Contains("Back to edit", StringComparison.Ordinal)).ClickAsync();

        // Assert
        documentsApiClient.PlacedHolds.ShouldBeEmpty();
        cut.Find("#hold-purpose").GetAttribute("value").ShouldBe("PURPOSE-9");
    }

    [Fact]
    public async Task A_refused_placement_shows_a_visible_reason_and_places_no_hold()
    {
        // Arrange
        documentsApiClient.PlaceHoldFailure = HttpOutcomeKind.Forbidden;
        var cut = HoldPageActions.Render(this, document.Id);
        await HoldPageActions.FillPlaceForm(cut);
        await cut.Find("form[aria-labelledby='place-hold-title']").SubmitAsync();

        // Act
        await cut.FindAll("button").ShouldHaveSingleItem(button => button.TextContent.Contains("Confirm placement", StringComparison.Ordinal)).ClickAsync();

        // Assert
        cut.Find(".alert-danger[role='alert']").TextContent.Trim().ShouldNotBeEmpty();
        cut.FindAll(".alert-success").ShouldBeEmpty();
        documentsApiClient.Holds.ShouldBeEmpty();
    }

    [Fact]
    public async Task Release_opens_an_accessible_modal_dialog_that_restates_document_and_hold_and_moves_focus_in()
    {
        // Arrange
        var hold = DocumentHoldTestData.Create();
        documentsApiClient.Holds.Add(hold);
        var cut = HoldPageActions.Render(this, document.Id);

        // Act
        await cut.Find($"button[aria-label='Release hold {hold.Id}']").ClickAsync();

        // Assert
        var dialog = cut.Find("[role='dialog']");
        dialog.GetAttribute("aria-modal").ShouldBe("true");
        var titleId = dialog.GetAttribute("aria-labelledby");
        cut.Find($"#{titleId}").TextContent.ShouldContain("Release document hold", StringComparison.Ordinal);
        var description = cut.Find($"#{dialog.GetAttribute("aria-describedby")}");
        description.TextContent.ShouldContain(document.Id.ToString(), StringComparison.Ordinal);
        description.TextContent.ShouldContain(hold.Id.ToString(), StringComparison.Ordinal);
        var input = cut.Find("#release-reason-code");
        input.GetAttribute("aria-required").ShouldBe("true");
        input.GetAttribute("aria-describedby").ShouldBe("release-reason-help");
        cut.Find("label[for='release-reason-code']").TextContent.ShouldBe("Release reason code");
        cut.Find("div.container-fluid").HasAttribute("inert").ShouldBeTrue();
        JSInterop.VerifyFocusAsyncInvoke();
    }

    [Fact]
    public async Task Releasing_without_a_valid_reason_code_shows_a_linked_error_and_calls_nothing()
    {
        // Arrange
        var hold = DocumentHoldTestData.Create();
        documentsApiClient.Holds.Add(hold);
        var cut = HoldPageActions.Render(this, document.Id);
        await cut.Find($"button[aria-label='Release hold {hold.Id}']").ClickAsync();

        // Act
        await cut.Find("[role='dialog'] form").SubmitAsync();

        // Assert
        documentsApiClient.ReleasedHolds.ShouldBeEmpty();
        cut.Find("#release-reason-error").TextContent.ShouldContain("required", StringComparison.Ordinal);
        cut.Find("#release-reason-code").GetAttribute("aria-invalid").ShouldBe("true");
        cut.Find("#release-reason-code").GetAttribute("aria-describedby").ShouldBe("release-reason-help release-reason-error");

        // Act
        await cut.Find("#release-reason-code").InputAsync(new ChangeEventArgs { Value = "free text with spaces" });
        await cut.Find("[role='dialog'] form").SubmitAsync();

        // Assert
        documentsApiClient.ReleasedHolds.ShouldBeEmpty();
        cut.Find("#release-reason-error").TextContent.ShouldContain("only letters, digits", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Confirming_a_release_sends_the_reason_code_and_shows_the_audit_outcome()
    {
        // Arrange
        var hold = DocumentHoldTestData.Create();
        documentsApiClient.Holds.Add(hold);
        var cut = HoldPageActions.Render(this, document.Id);
        await cut.Find($"button[aria-label='Release hold {hold.Id}']").ClickAsync();
        await cut.Find("#release-reason-code").InputAsync(new ChangeEventArgs { Value = "  CLAIM-CLOSED  " });

        // Act
        await cut.Find("[role='dialog'] form").SubmitAsync();

        // Assert
        var (documentId, holdId, dto) = documentsApiClient.ReleasedHolds.ShouldHaveSingleItem();
        documentId.ShouldBe(document.Id);
        holdId.ShouldBe(hold.Id);
        dto.ReasonCode.ShouldBe("CLAIM-CLOSED");
        cut.FindAll("[role='dialog']").ShouldBeEmpty();
        cut.Find("div.container-fluid").HasAttribute("inert").ShouldBeFalse();
        var status = cut.Find(".alert-success");
        status.TextContent.ShouldContain("released by actor-release", StringComparison.Ordinal);
        status.TextContent.ShouldContain("audit trail", StringComparison.Ordinal);
        cut.Find("tbody").TextContent.ShouldContain("Released", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Escape_cancels_the_release_dialog_without_releasing_and_returns_focus_to_the_opener()
    {
        // Arrange
        var hold = DocumentHoldTestData.Create();
        documentsApiClient.Holds.Add(hold);
        var cut = HoldPageActions.Render(this, document.Id);
        await cut.Find($"button[aria-label='Release hold {hold.Id}']").ClickAsync();

        // Act
        await cut.Find("[role='dialog']").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        // Assert
        cut.FindAll("[role='dialog']").ShouldBeEmpty();
        documentsApiClient.ReleasedHolds.ShouldBeEmpty();
        cut.Find("div.container-fluid").HasAttribute("inert").ShouldBeFalse();
        var focusInvocations = JSInterop.Invocations.Where(invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus").ToArray();
        focusInvocations.Length.ShouldBe(2);
    }

    [Fact]
    public async Task The_cancel_button_closes_the_release_dialog_without_releasing()
    {
        // Arrange
        var hold = DocumentHoldTestData.Create();
        documentsApiClient.Holds.Add(hold);
        var cut = HoldPageActions.Render(this, document.Id);
        await cut.Find($"button[aria-label='Release hold {hold.Id}']").ClickAsync();

        // Act
        await cut.Find("[role='dialog'] button.btn-secondary").ClickAsync();

        // Assert
        cut.FindAll("[role='dialog']").ShouldBeEmpty();
        documentsApiClient.ReleasedHolds.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpOutcomeKind.Forbidden)]
    [InlineData(HttpOutcomeKind.Conflict)]
    public async Task A_refused_release_keeps_the_dialog_open_with_a_visible_reason(HttpOutcomeKind failure)
    {
        // Arrange
        var hold = DocumentHoldTestData.Create();
        documentsApiClient.Holds.Add(hold);
        documentsApiClient.ReleaseHoldFailure = failure;
        var cut = HoldPageActions.Render(this, document.Id);
        await cut.Find($"button[aria-label='Release hold {hold.Id}']").ClickAsync();
        await cut.Find("#release-reason-code").InputAsync(new ChangeEventArgs { Value = "CLAIM-CLOSED" });

        // Act
        await cut.Find("[role='dialog'] form").SubmitAsync();

        // Assert
        cut.Find("[role='dialog'] .alert-danger[role='alert']").TextContent.Trim().ShouldNotBeEmpty();
        cut.FindAll(".alert-success").ShouldBeEmpty();
    }
}
