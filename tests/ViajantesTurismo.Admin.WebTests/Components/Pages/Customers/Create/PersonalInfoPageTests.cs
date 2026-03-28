using ViajantesTurismo.Admin.Web;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers.Create;
using ViajantesTurismo.Admin.WebTests.Infrastructure;

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
        Services.AddSingleton<CustomerCreationState>();
        Services.AddSingleton<CountryService>(new FakeCountryService());
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

    [Fact]
    public async Task Renders_All_Required_Personal_Info_Controls()
    {
        // Arrange
        var cut = Render<PersonalInfo>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        Assert.NotNull(cut.Find("input#firstName"));
        Assert.NotNull(cut.Find("input#lastName"));
        Assert.NotNull(cut.Find("input#birthDate"));
        Assert.NotNull(cut.Find("select#gender"));
        Assert.NotNull(cut.Find("#nationality"));
        Assert.NotNull(cut.Find("input#occupation"));
    }
}
