using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Management.Web.Components.Pages.Customers.Create;
using ViajantesTurismo.Management.WebTests.Infrastructure;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Create;

/// <summary>
/// Tests for the PersonalInfo wizard step page.
/// Validates that validation messages are shown only via inline ValidationMessage
/// components and NOT duplicated via a ValidationSummary.
/// </summary>
public sealed class PersonalInfoPageTests : BunitContext
{
    public PersonalInfoPageTests()
    {
        Services.AddSingleton<CustomerCreationState>();
        Services.AddSingleton<ICountryService>(new FakeCountryService());
    }

    [Fact]
    public async Task Submit_empty_form_shows_inline_errors_without_validation_summary()
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

    [Fact]
    public async Task Renders_all_required_personal_info_controls()
    {
        // Arrange
        var cut = Render<PersonalInfo>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        Assert.NotNull(cut.Find("input#firstName"));
        Assert.NotNull(cut.Find("input#lastName"));
        Assert.NotNull(cut.Find("input#birthDate"));
        Assert.NotNull(cut.Find("select#gender"));
        Assert.NotNull(cut.Find("label[for='nationality']"));
        Assert.NotNull(cut.Find("#nationality"));
        Assert.NotNull(cut.Find("input#occupation"));
    }

    [Fact]
    public void Renders_accessible_country_loading_status_until_countries_are_available()
    {
        // Arrange
        var countries = new TaskCompletionSource<CountryInfo[]>();
        Services.AddSingleton<ICountryService>(new FakeCountryService(countries.Task));

        // Act
        var cut = Render<PersonalInfo>();

        // Assert
        var status = cut.Find("[role='status'][aria-live='polite'][aria-busy='true']");
        Assert.Contains("Loading countries", status.TextContent, StringComparison.Ordinal);
        Assert.NotNull(status.QuerySelector(".spinner-border[aria-hidden='true']"));
        Assert.Empty(cut.FindAll("#nationality"));
    }
}
