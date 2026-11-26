using SharedKernel.Blazor;
using SharedKernel.Testing;
using ViajantesTurismo.Management.WebTests.Infrastructure;
using Add = ViajantesTurismo.Management.Web.Components.Pages.Tours.Add;
using Edit = ViajantesTurismo.Management.Web.Components.Pages.Tours.Edit;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Tours;

[TestScope(TestScope.Component)]
public class TourFormLabelTests : BunitContext
{
    private readonly FakeToursApiClient _fakeToursApi = new();

    public TourFormLabelTests()
    {
        Services.AddSingleton<IUiFailureReporter>(new RecordingUiFailureReporter());
        Services.AddSingleton(TimeProvider.System);
        Services.AddSingleton<IToursApiClient>(_fakeToursApi);
    }

    [Theory]
    [Trait(TestTraitNames.CategoryName, "Regression")]
    [InlineData("startDate", "input", "Start Date")]
    [InlineData("endDate", "input", "End Date")]
    [InlineData("price", "input", "Base Price")]
    [InlineData("singleRoom", "input", "Single Room Supplement")]
    [InlineData("regularBike", "input", "Regular Bike Price")]
    [InlineData("eBike", "input", "E-Bike Price")]
    [InlineData("services", "textarea", "Included Services")]
    [InlineData("minCustomers", "input", "Minimum Customers")]
    [InlineData("maxCustomers", "input", "Maximum Customers")]
    public async Task Create_and_edit_tour_labels_identify_their_controls(string fieldId, string controlTag, string caption)
    {
        // Arrange
        var tour = await EditPageTestsHelper.CreateTestTour(_fakeToursApi);

        // Act
        var createPage = Render<Add>();
        var editPage = Render<Edit>(parameters => parameters.Add(page => page.Id, tour.Id));
        await editPage.WaitForStateAsync(() => editPage.FindAll("input#identifier").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var controlSelector = $"{controlTag}#{fieldId}";
        var labelSelector = $"label.form-label[for='{fieldId}']";
        createPage.FindAll(controlSelector).ShouldHaveSingleItem();
        editPage.FindAll(controlSelector).ShouldHaveSingleItem();
        createPage.FindAll(labelSelector).ShouldHaveSingleItem().TextContent.ShouldBe(caption);
        editPage.FindAll(labelSelector).ShouldHaveSingleItem().TextContent.ShouldBe(caption);
    }
}
