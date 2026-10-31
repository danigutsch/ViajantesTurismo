using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Browser keyboard journey for releasing a document hold through the modal dialog.</summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.System)]
public sealed class ReleaseHoldKeyboardTests : ManagementWebBrowserTestBase
{
    private readonly GetDocumentDto document = new()
    {
        Id = Guid.CreateVersion7(),
        BookingId = Guid.CreateVersion7(),
        Revision = 1,
        TemplateId = "tour-service-contract",
        TemplateVersion = "1",
        SourceVersion = "SOURCE-VERSION",
        Status = DocumentStatusDto.Finalized,
        Fields = [],
        CreatedAt = TimeProvider.System.GetUtcNow().UtcDateTime,
        UpdatedAt = TimeProvider.System.GetUtcNow().UtcDateTime,
        FinalizedAt = TimeProvider.System.GetUtcNow().UtcDateTime,
        HasFinalizedArtifact = false
    };

    private readonly FakeDocumentsBrowserApiClient documents;
    private readonly GetDocumentHoldDto hold;

    public ReleaseHoldKeyboardTests()
    {
        var now = TimeProvider.System.GetUtcNow();
        hold = new GetDocumentHoldDto
        {
            Id = Guid.CreateVersion7(),
            Basis = "LegalClaim",
            PurposeCode = "PURPOSE-1",
            AuthorityReference = "AUTH-1",
            PlacedBy = "actor-place",
            PlacedAt = now.AddDays(-5),
            EffectiveFrom = now.AddDays(-5),
            ReviewDueAt = now.AddDays(60),
            IsRestraining = true
        };
        documents = new FakeDocumentsBrowserApiClient(document, hold);
        Registrations.AddSingleton<IDocumentsApiClient>(documents);
    }

    [Fact]
    public async Task Escape_cancels_the_release_dialog_and_returns_focus_to_the_release_button()
    {
        // Arrange
        await NavigateTo($"/documents/{document.Id}");
        var opener = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = $"Release hold {hold.Id}" });
        await opener.FocusAsync();

        // Act
        await Page.Keyboard.PressAsync("Enter");
        var dialog = Page.GetByRole(AriaRole.Dialog, new PageGetByRoleOptions { Name = "Release document hold" });
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog.GetByLabel("Release reason code")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await Expect(dialog).ToBeHiddenAsync();
        await Expect(opener).ToBeFocusedAsync();
        documents.ReleaseReasonCodes.ShouldBeEmpty();
    }

    [Fact]
    public async Task Tab_stays_inside_the_dialog_and_enter_in_the_reason_field_releases_the_hold()
    {
        // Arrange
        await NavigateTo($"/documents/{document.Id}");
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = $"Release hold {hold.Id}" }).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        var dialog = Page.GetByRole(AriaRole.Dialog, new PageGetByRoleOptions { Name = "Release document hold" });
        var reason = dialog.GetByLabel("Release reason code");
        await Expect(reason).ToBeFocusedAsync();

        // Act
        await Page.Keyboard.PressAsync("Tab");
        await Expect(dialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Cancel" })).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(dialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Release hold" })).ToBeFocusedAsync();
        await reason.FocusAsync();
        await reason.FillAsync("CLAIM-CLOSED");
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new LocatorFilterOptions { HasText = "released by actor-release" })).ToBeVisibleAsync();
        await Expect(dialog).ToBeHiddenAsync();
        documents.ReleaseReasonCodes.ShouldHaveSingleItem().ShouldBe("CLAIM-CLOSED");
    }
}
