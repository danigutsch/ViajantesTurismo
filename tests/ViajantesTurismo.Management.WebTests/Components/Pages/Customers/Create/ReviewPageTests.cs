using Microsoft.AspNetCore.Components;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Management.Web.Components.Pages.Customers.Create;
using ViajantesTurismo.Management.Web.Models;
using ContractCommandOutcomeDto = SharedKernel.HttpClients.ContractCommandOutcomeDto;
using ContractCommandOutcomeKind = SharedKernel.HttpClients.ContractCommandOutcomeKind;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Create;

[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
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
            (alert.TextContent).ShouldContain("We couldn't create the customer right now. Please try again.", StringComparison.Ordinal);
            (alert.TextContent).ShouldNotContain("Backend is on vacation", StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task SubmitCustomer_when_create_returns_non_success_outcome_shows_sanitized_error_message()
    {
        // Arrange
        CustomerCreationStateTestHelper.SeedCompletedState(_state);
        _fakeCustomersApi.SetCreateCustomerOutcome(new ContractCommandOutcomeDto
        {
            Kind = ContractCommandOutcomeKind.Conflict,
            StatusCode = System.Net.HttpStatusCode.Conflict,
            Message = "Duplicate email"
        });

        var cut = Render<Review>();

        // Act
        var submitButton = cut.FindAll("button")
            .First(button => button.TextContent.Contains("Create Customer", StringComparison.Ordinal));
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var alert = cut.Find(".alert.alert-danger");
            alert.TextContent.ShouldContain("We couldn't create the customer right now. Please try again.", StringComparison.Ordinal);
            alert.TextContent.ShouldNotContain("Duplicate email", StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task When_state_is_incomplete_shows_warning_and_go_to_step_1_button_navigates()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Review>();

        // Assert
        await cut.WaitForAssertionAsync(() => (cut.Markup).ShouldContain("Please complete all steps before submitting.", StringComparison.Ordinal));

        // Act
        var goToStepOneButton = cut.FindAll("button")
            .First(button => button.TextContent.Contains("Go to Step 1", StringComparison.Ordinal));
        await cut.InvokeAsync(() => goToStepOneButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() => (navigationManager.Uri).ShouldEndWith("/customers/create/personal-info", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null, BedTypeDto.SingleBed)]
    [InlineData(RoomTypeDto.DoubleOccupancy, null)]
    public void Missing_required_accommodation_value_shows_incomplete_warning(
        RoomTypeDto? roomType,
        BedTypeDto? bedType)
    {
        // Arrange
        CustomerCreationStateTestHelper.SeedCompletedState(_state);
        _state.SetAccommodationPreferences(new AccommodationPreferencesFormModel
        {
            RoomType = roomType,
            BedType = bedType,
        });

        // Act
        var cut = Render<Review>();

        // Assert
        cut.Markup.ShouldContain("Please complete all steps before submitting.", StringComparison.Ordinal);
        cut.FindAll("button.btn-success").ShouldBeEmpty();
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
            (cut.Markup).ShouldContain("None reported", StringComparison.Ordinal);
            (cut.Markup).ShouldNotContain("Instagram:", StringComparison.Ordinal);
            (cut.Markup).ShouldNotContain("Facebook:", StringComparison.Ordinal);
            (cut.Markup).ShouldNotContain("Companion ID:", StringComparison.Ordinal);
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
            (cut.Markup).ShouldContain("Instagram:", StringComparison.Ordinal);
            (cut.Markup).ShouldContain("Facebook:", StringComparison.Ordinal);
            (cut.Markup).ShouldContain("Companion ID:", StringComparison.Ordinal);
            (cut.Markup).ShouldContain("11111111-1111-1111-1111-111111111111", StringComparison.Ordinal);
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
        await cut.WaitForAssertionAsync(() => (navigationManager.Uri).ShouldContain("/customers/", StringComparison.Ordinal));
        (_state.IsComplete()).ShouldBeFalse();
        (_state.CurrentStep).ShouldBe(1);
        (_state.PersonalInfo).ShouldBeNull();
        (_state.MedicalInfo).ShouldBeNull();
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
        await cut.WaitForAssertionAsync(() => navigationManager.Uri.ShouldEndWith("/customers/absolute-id?source=review", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("customers/relative-id", "/customers/relative-id")]
    [InlineData("/api/v1/customers/relative-id", "/customers/relative-id")]
    [InlineData("/api/v1/customerz", "/api/v1/customerz")]
    [InlineData("/api/v1/customers-malformed/relative-id", "/api/v1/customers-malformed/relative-id")]
    [InlineData("//evil.example/customers/relative-id", "/customers")]
    public async Task SubmitCustomer_when_create_succeeds_with_relative_location_navigates_to_app_local_path(
        string location,
        string expectedPath)
    {
        // Arrange
        CustomerCreationStateTestHelper.SeedCompletedState(_state);
        _fakeCustomersApi.SetCreateCustomerOutcome(new ContractCommandOutcomeDto
        {
            Kind = ContractCommandOutcomeKind.Succeeded,
            StatusCode = System.Net.HttpStatusCode.Created,
            Location = new Uri(location, UriKind.Relative)
        });
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Review>();

        // Act
        var submitButton = cut.FindAll("button")
            .First(button => button.TextContent.Contains("Create Customer", StringComparison.Ordinal));
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() => navigationManager.Uri.ShouldEndWith(expectedPath, StringComparison.Ordinal));
    }

    [Fact]
    public async Task SubmitCustomer_when_create_succeeds_without_location_navigates_to_customers_fallback()
    {
        // Arrange
        CustomerCreationStateTestHelper.SeedCompletedState(_state);
        _fakeCustomersApi.SetCreateCustomerOutcome(new ContractCommandOutcomeDto
        {
            Kind = ContractCommandOutcomeKind.Succeeded,
            StatusCode = System.Net.HttpStatusCode.Created
        });
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Review>();

        // Act
        var submitButton = cut.FindAll("button")
            .First(button => button.TextContent.Contains("Create Customer", StringComparison.Ordinal));
        await cut.InvokeAsync(() => submitButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() => navigationManager.Uri.ShouldEndWith("/customers", StringComparison.Ordinal));
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
        await cut.WaitForAssertionAsync(() => (navigationManager.Uri).ShouldEndWith("/customers/create/medical", StringComparison.Ordinal));
        (_state.CurrentStep).ShouldBe(8);
    }

}
