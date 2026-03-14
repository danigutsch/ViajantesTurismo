using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using ViajantesTurismo.Admin.Web;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers.Create;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers.Create;

/// <summary>
/// Tests for the PersonalInfo wizard step page.
/// Validates that validation messages are shown only via inline ValidationMessage
/// components and NOT duplicated via a ValidationSummary.
/// </summary>
public sealed class PersonalInfoPageTests : BunitContext
{
    public PersonalInfoPageTests()
    {
        // Create a temp directory with an empty countries.json to satisfy CountryService
        var webRoot = Path.Combine(Path.GetTempPath(), "PersonalInfoPageTests");
        var dataDir = Path.Combine(webRoot, "data");
        Directory.CreateDirectory(dataDir);
        File.WriteAllText(Path.Combine(dataDir, "countries.json"), "{}");

        Services.AddSingleton<CustomerCreationState>();
        Services.AddSingleton<IWebHostEnvironment>(new StubWebHostEnvironment { WebRootPath = webRoot });
        Services.AddSingleton<CountryService>();
    }

    [Fact]
    public async Task Submit_Empty_Form_Shows_Inline_Errors_Without_Validation_Summary()
    {
        // Arrange — render the page (triggers OnInitializedAsync)
        var cut = Render<PersonalInfo>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Act — submit form with empty required fields to trigger validation
        var form = cut.Find("form");
        await form.SubmitAsync();

        // Assert — inline ValidationMessage elements (rendered as <div>) should be present
        var inlineMessages = cut.FindAll("div.validation-message");
        Assert.NotEmpty(inlineMessages);

        // Assert — ValidationSummary (rendered as <ul class="alert alert-danger">)
        // should NOT be present to avoid duplicating the inline messages (BUG-004).
        var validationSummaries = cut.FindAll("ul.alert-danger");
        Assert.Empty(validationSummaries);
    }

    /// <summary>
    /// Minimal IWebHostEnvironment stub for CountryService.
    /// CountryService catches FileNotFoundException when countries.json is missing.
    /// </summary>
    private sealed class StubWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "TestApp";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Testing";
    }
}
