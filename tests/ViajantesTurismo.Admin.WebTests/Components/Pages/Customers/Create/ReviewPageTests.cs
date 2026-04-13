using Microsoft.AspNetCore.Components;
using ViajantesTurismo.Admin.Web;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers.Create;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers.Create;

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
    public async Task SubmitCustomer_When_Create_Fails_Shows_Sanitized_Error_Message()
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
    public async Task When_State_Is_Incomplete_Shows_Warning_And_Go_To_Step_1_Button_Navigates()
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
    public void Complete_State_With_Optional_Values_Missing_Shows_Fallbacks_And_Hides_Optional_Sections()
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
    public async Task SubmitCustomer_When_Create_Succeeds_Resets_State_And_Navigates_To_Customer_Details()
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
}
