using Microsoft.AspNetCore.Components;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Management.Web.Components.Pages.Customers.Create;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Create;

public sealed class ReviewPageTests : BunitContext
{
    private readonly FakeCustomersApiClient _fakeCustomersApi = new();
    private readonly CustomerCreationState _state = new();

    public ReviewPageTests()
    {
        Services.AddSingleton(_state);
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
    }

    [Fact]
    public async Task SubmitCustomer_when_create_fails_shows_sanitized_error_message()
    {
        // Arrange
        CustomerCreationStateTestHelper.SeedCompletedState(_state);
        _fakeCustomersApi.SetCreateCustomerException(new InvalidOperationException("Backend is on vacation"));

        var cut = Render<Review>();

        // Act
        var submitButton = cut.FindAll("button")
            .First(button => button.TextContent.Contains("Create Customer", StringComparison.Ordinal));
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var alert = cut.Find(".alert.alert-danger");
            Assert.Contains("We couldn't create the customer right now. Please try again.", alert.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Backend is on vacation", alert.TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task When_state_is_incomplete_shows_warning_and_go_to_step_1_button_navigates()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Review>();

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.Contains("Please complete all steps before submitting.", cut.Markup, StringComparison.Ordinal));

        // Act
        var goToStepOneButton = cut.FindAll("button")
            .First(button => button.TextContent.Contains("Go to Step 1", StringComparison.Ordinal));
        await cut.InvokeAsync(() => goToStepOneButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.EndsWith("/customers/create/personal-info", navigationManager.Uri, StringComparison.Ordinal));
    }

    [Fact]
    public void Complete_state_with_optional_values_missing_shows_fallbacks_and_hides_optional_sections()
    {
        // Arrange
        CustomerCreationStateTestHelper.SeedCompletedState(
            _state,
            includeOptionalSocials: false,
            includeCompanion: false,
            includeMedicalDetails: false);

        // Act
        var cut = Render<Review>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("None reported", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Instagram:", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Facebook:", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Companion ID:", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Complete_state_with_optional_values_present_shows_socials_and_companion_id()
    {
        // Arrange
        CustomerCreationStateTestHelper.SeedCompletedState(
            _state,
            includeOptionalSocials: true,
            includeCompanion: true,
            includeMedicalDetails: true);

        // Act
        var cut = Render<Review>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Instagram:", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Facebook:", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Companion ID:", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("11111111-1111-1111-1111-111111111111", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task SubmitCustomer_when_create_succeeds_resets_state_and_navigates_to_customer_details()
    {
        // Arrange
        CustomerCreationStateTestHelper.SeedCompletedState(_state);
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Review>();

        // Act
        var submitButton = cut.FindAll("button")
            .First(button => button.TextContent.Contains("Create Customer", StringComparison.Ordinal));
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.Contains("/customers/", navigationManager.Uri, StringComparison.Ordinal));
        Assert.False(_state.IsComplete());
        Assert.Equal(1, _state.CurrentStep);
        Assert.Null(_state.PersonalInfo);
        Assert.Null(_state.MedicalInfo);
    }

    [Fact]
    public async Task SubmitCustomer_when_create_succeeds_with_absolute_location_navigates_using_path_and_query()
    {
        // Arrange
        CustomerCreationStateTestHelper.SeedCompletedState(_state);
        Services.AddSingleton<ICustomersApiClient>(new AbsoluteLocationCustomersApiClient());
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Review>();

        // Act
        var submitButton = cut.FindAll("button")
            .First(button => button.TextContent.Contains("Create Customer", StringComparison.Ordinal));
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.EndsWith("/customers/absolute-id?source=review", navigationManager.Uri, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Back_button_navigates_to_medical_and_keeps_wizard_on_step_8()
    {
        // Arrange
        CustomerCreationStateTestHelper.SeedCompletedState(_state);
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Review>();

        // Act
        var backButton = cut.FindAll("button")
            .First(button => button.TextContent.Contains("Back", StringComparison.Ordinal));
        await cut.InvokeAsync(() => backButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.EndsWith("/customers/create/medical", navigationManager.Uri, StringComparison.Ordinal));
        Assert.Equal(8, _state.CurrentStep);
    }

}
