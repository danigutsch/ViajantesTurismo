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
    public void Complete_State_With_Optional_Values_Present_Shows_Socials_And_Companion_Id()
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

    [Fact]
    public async Task SubmitCustomer_When_Create_Succeeds_With_Absolute_Location_Navigates_Using_Path_And_Query()
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
    public async Task Back_Button_Navigates_To_Medical_And_Keeps_Wizard_On_Step_8()
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

    private sealed class AbsoluteLocationCustomersApiClient : ICustomersApiClient
    {
        public Task<IReadOnlyList<GetCustomerDto>> GetCustomers(CancellationToken cancellationToken, int maxItems = 100) => throw new NotImplementedException();

        public Task<CustomerDetailsDto?> GetCustomerById(Guid id, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<Uri> CreateCustomer(CreateCustomerDto dto, CancellationToken cancellationToken) =>
            Task.FromResult(new Uri("https://example.test/customers/absolute-id?source=review", UriKind.Absolute));

        public Task UpdateCustomer(Guid id, UpdateCustomerDto dto, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<ImportResultDto> ImportCustomers(byte[] fileContent, string fileName, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<ImportResultDto> CommitImportWithResolutions(byte[] fileContent, string fileName, IReadOnlyDictionary<string, string> conflictResolutions, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
