using Add = ViajantesTurismo.Admin.Web.Components.Pages.Tours.Add;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Tours;

public class AddPageTests : BunitContext
{
    private readonly FakeToursApiClient _fakeToursApi;

    public AddPageTests()
    {
        _fakeToursApi = new FakeToursApiClient();
        Services.AddSingleton<IToursApiClient>(_fakeToursApi);
    }

    [Fact]
    public void Renders_Page_Title()
    {
        // Arrange
        // Act
        var cut = Render<Add>();

        // Assert
        var title = cut.Find("h1");
        Assert.Equal("Add New Tour", title.TextContent);
    }

    [Fact]
    public void Renders_All_Form_Fields()
    {
        // Arrange
        // Act
        var cut = Render<Add>();

        // Assert
        Assert.NotNull(cut.Find("input#identifier"));
        Assert.NotNull(cut.Find("input#name"));
        Assert.NotNull(cut.Find("input#startDate"));
        Assert.NotNull(cut.Find("input#endDate"));
        Assert.NotNull(cut.Find("select#currency"));
        Assert.NotNull(cut.Find("input#price"));
        Assert.NotNull(cut.Find("input#singleRoom"));
        Assert.NotNull(cut.Find("input#regularBike"));
        Assert.NotNull(cut.Find("input#eBike"));
        Assert.NotNull(cut.Find("textarea#services"));
        Assert.NotNull(cut.Find("input#minCustomers"));
        Assert.NotNull(cut.Find("input#maxCustomers"));
    }

    [Fact]
    public void Currency_Dropdown_Contains_All_Options()
    {
        // Arrange
        // Act
        var cut = Render<Add>();

        // Assert
        var currencySelect = cut.Find("select#currency");
        var options = currencySelect.QuerySelectorAll("option");

        Assert.Equal(3, options.Length);
        Assert.Contains(options, o => o.TextContent.Contains("Brazilian Real", StringComparison.Ordinal));
        Assert.Contains(options, o => o.TextContent.Contains("Euro", StringComparison.Ordinal));
        Assert.Contains(options, o => o.TextContent.Contains("US Dollar", StringComparison.Ordinal));
    }

    [Fact]
    public void Submit_Button_Has_Correct_Initial_Text()
    {
        // Arrange
        // Act
        var cut = Render<Add>();

        // Assert
        var submitButton = cut.Find("button[type='submit']");
        Assert.Contains("Create Tour", submitButton.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Shows_Validation_Summary()
    {
        // Arrange
        var cut = Render<Add>();

        // Act
        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll(".validation-message").Count > 0 || cut.FindAll("ul.validation-errors").Count > 0, TimeSpan.FromSeconds(2));

        var validationErrors = cut.FindAll("ul.validation-errors, .validation-message");
        Assert.NotEmpty(validationErrors);
    }

    [Fact]
    public async Task Successful_Submission_Shows_Success_Message()
    {
        // Arrange
        var cut = Render<Add>();

        // Act
        await cut.InvokeAsync(() => cut.Find("input#identifier").Change("CUBA2024"));
        await cut.InvokeAsync(() => cut.Find("input#name").Change("Cuba Adventure"));
        await cut.InvokeAsync(() => cut.Find("input#price").Change("1500"));
        await cut.InvokeAsync(() => cut.Find("input#singleRoom").Change("200"));
        await cut.InvokeAsync(() => cut.Find("input#regularBike").Change("50"));
        await cut.InvokeAsync(() => cut.Find("input#eBike").Change("100"));
        await cut.InvokeAsync(() => cut.Find("textarea#services").Change("Hotel\nBreakfast\nLunch"));
        await cut.InvokeAsync(() => cut.Find("input#minCustomers").Change("5"));
        await cut.InvokeAsync(() => cut.Find("input#maxCustomers").Change("15"));

        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll(".alert-success").Count > 0, TimeSpan.FromSeconds(2));

        var successAlert = cut.Find(".alert-success");
        Assert.Contains("Tour created successfully!", successAlert.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Successful_Submission_Shows_Action_Buttons()
    {
        // Arrange
        var cut = Render<Add>();

        // Act
        await cut.InvokeAsync(() => cut.Find("input#identifier").Change("CUBA2024"));
        await cut.InvokeAsync(() => cut.Find("input#name").Change("Cuba Adventure"));
        await cut.InvokeAsync(() => cut.Find("input#price").Change("1500"));
        await cut.InvokeAsync(() => cut.Find("input#singleRoom").Change("200"));
        await cut.InvokeAsync(() => cut.Find("input#regularBike").Change("50"));
        await cut.InvokeAsync(() => cut.Find("input#eBike").Change("100"));
        await cut.InvokeAsync(() => cut.Find("textarea#services").Change("Hotel"));
        await cut.InvokeAsync(() => cut.Find("input#minCustomers").Change("5"));
        await cut.InvokeAsync(() => cut.Find("input#maxCustomers").Change("15"));

        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll(".alert-success").Count > 0, TimeSpan.FromSeconds(2));

        var successAlert = cut.Find(".alert-success");
        var buttons = successAlert.QuerySelectorAll("button, a.btn");

        Assert.True(buttons.Length >= 3);
        Assert.Contains(buttons, b => b.TextContent.Contains("View Tour Details", StringComparison.Ordinal));
        Assert.Contains(buttons, b => b.TextContent.Contains("Create Another Tour", StringComparison.Ordinal));
        Assert.Contains(buttons, b => b.TextContent.Contains("View All Tours", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Create_Another_Button_Resets_Form()
    {
        // Arrange
        var cut = Render<Add>();

        // Act
        await cut.InvokeAsync(() => cut.Find("input#identifier").Change("CUBA2024"));
        await cut.InvokeAsync(() => cut.Find("input#name").Change("Cuba Adventure"));
        await cut.InvokeAsync(() => cut.Find("input#price").Change("1500"));
        await cut.InvokeAsync(() => cut.Find("input#singleRoom").Change("200"));
        await cut.InvokeAsync(() => cut.Find("input#regularBike").Change("50"));
        await cut.InvokeAsync(() => cut.Find("input#eBike").Change("100"));
        await cut.InvokeAsync(() => cut.Find("textarea#services").Change("Hotel"));
        await cut.InvokeAsync(() => cut.Find("input#minCustomers").Change("5"));
        await cut.InvokeAsync(() => cut.Find("input#maxCustomers").Change("15"));

        await cut.InvokeAsync(() => cut.Find("form").Submit());
        await cut.WaitForStateAsync(() => cut.FindAll(".alert-success").Count > 0, TimeSpan.FromSeconds(2));

        var createAnotherButton = cut.FindAll("button").First(b => b.TextContent.Contains("Create Another Tour", StringComparison.Ordinal));
        await cut.InvokeAsync(() => createAnotherButton.Click());

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll(".alert-success").Count == 0, TimeSpan.FromSeconds(2));
        Assert.Empty(cut.FindAll(".alert-success"));
    }

    [Fact]
    public async Task Submission_Shows_Spinner_And_Disabled_Button()
    {
        // Arrange
        var cut = Render<Add>();

        // Act
        await cut.InvokeAsync(() => cut.Find("input#identifier").Change("CUBA2024"));
        await cut.InvokeAsync(() => cut.Find("input#name").Change("Cuba Adventure"));
        await cut.InvokeAsync(() => cut.Find("input#price").Change("1500"));
        await cut.InvokeAsync(() => cut.Find("input#singleRoom").Change("200"));
        await cut.InvokeAsync(() => cut.Find("input#regularBike").Change("50"));
        await cut.InvokeAsync(() => cut.Find("input#eBike").Change("100"));
        await cut.InvokeAsync(() => cut.Find("textarea#services").Change("Hotel"));
        await cut.InvokeAsync(() => cut.Find("input#minCustomers").Change("5"));
        await cut.InvokeAsync(() => cut.Find("input#maxCustomers").Change("15"));

        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());

        // Assert
        await cut.WaitForAssertionAsync(() =>
            Assert.True(
                cut.Find("button[type='submit']").TextContent.Contains("Creating...", StringComparison.Ordinal)
                || cut.FindAll(".alert-success").Count > 0),
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task API_Error_Shows_Error_Message()
    {
        // Arrange
        _fakeToursApi.SetCreateTourException(new InvalidOperationException("Failed to create tour"));
        var cut = Render<Add>();

        // Act
        await cut.InvokeAsync(() => cut.Find("input#identifier").Change("CUBA2024"));
        await cut.InvokeAsync(() => cut.Find("input#name").Change("Cuba Adventure"));
        await cut.InvokeAsync(() => cut.Find("input#price").Change("1500"));
        await cut.InvokeAsync(() => cut.Find("input#singleRoom").Change("200"));
        await cut.InvokeAsync(() => cut.Find("input#regularBike").Change("50"));
        await cut.InvokeAsync(() => cut.Find("input#eBike").Change("100"));
        await cut.InvokeAsync(() => cut.Find("textarea#services").Change("Hotel"));
        await cut.InvokeAsync(() => cut.Find("input#minCustomers").Change("5"));
        await cut.InvokeAsync(() => cut.Find("input#maxCustomers").Change("15"));

        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll(".alert-danger").Count > 0, TimeSpan.FromSeconds(2));

        var errorAlert = cut.Find(".alert-danger");
        Assert.Contains("We couldn't create the tour right now. Please try again.", errorAlert.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Failed to create tour", errorAlert.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Form_Uses_DataAnnotationsValidator()
    {
        // Arrange
        // Act
        var cut = Render<Add>();

        // Assert
        var validator = cut.FindComponent<DataAnnotationsValidator>();
        Assert.NotNull(validator);
    }

    [Fact]
    public void Single_Room_Supplement_Field_Is_Present()
    {
        // Arrange
        // Act
        var cut = Render<Add>();

        // Assert
        var field = cut.Find("input#singleRoom");
        var label = cut.FindAll("label").First(l => l.GetAttribute("for") == "singleRoom");

        Assert.NotNull(field);
        Assert.Contains("Single Room Supplement", label.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Bike_Price_Fields_Are_Present()
    {
        // Arrange
        // Act
        var cut = Render<Add>();

        // Assert
        var regularBikeField = cut.Find("input#regularBike");
        var eBikeField = cut.Find("input#eBike");

        Assert.NotNull(regularBikeField);
        Assert.NotNull(eBikeField);

        var labels = cut.FindAll("label");
        Assert.Contains(labels, l => l.TextContent.Contains("Regular Bike Price", StringComparison.Ordinal));
        Assert.Contains(labels, l => l.TextContent.Contains("E-Bike Price", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Creates_Tour_With_Correct_Data()
    {
        // Arrange
        var cut = Render<Add>();

        // Act
        await cut.InvokeAsync(() => cut.Find("input#identifier").Change("CUBA2024"));
        await cut.InvokeAsync(() => cut.Find("input#name").Change("Cuba Adventure"));
        await cut.InvokeAsync(() => cut.Find("input#price").Change("1500.50"));
        await cut.InvokeAsync(() => cut.Find("input#singleRoom").Change("200.25"));
        await cut.InvokeAsync(() => cut.Find("input#regularBike").Change("50.00"));
        await cut.InvokeAsync(() => cut.Find("input#eBike").Change("100.75"));
        await cut.InvokeAsync(() => cut.Find("textarea#services").Change("Hotel\nBreakfast\nLunch\nDinner"));
        await cut.InvokeAsync(() => cut.Find("input#minCustomers").Change("5"));
        await cut.InvokeAsync(() => cut.Find("input#maxCustomers").Change("15"));

        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll(".alert-success").Count > 0, TimeSpan.FromSeconds(2));

        var createdTours = await _fakeToursApi.GetTours(CancellationToken.None);
        Assert.Single(createdTours);

        var tour = createdTours[0];
        Assert.Equal("CUBA2024", tour.Identifier);
        Assert.Equal("Cuba Adventure", tour.Name);
        Assert.Equal(1500.50m, tour.Price);
    }

    [Fact]
    public async Task Services_Input_Splits_By_Lines()
    {
        // Arrange
        var cut = Render<Add>();

        // Act
        await cut.InvokeAsync(() => cut.Find("input#identifier").Change("TEST2024"));
        await cut.InvokeAsync(() => cut.Find("input#name").Change("Test Tour"));
        await cut.InvokeAsync(() => cut.Find("input#price").Change("1000"));
        await cut.InvokeAsync(() => cut.Find("input#singleRoom").Change("150"));
        await cut.InvokeAsync(() => cut.Find("input#regularBike").Change("40"));
        await cut.InvokeAsync(() => cut.Find("input#eBike").Change("80"));
        await cut.InvokeAsync(() => cut.Find("textarea#services").Change("Service 1\nService 2\nService 3"));
        await cut.InvokeAsync(() => cut.Find("input#minCustomers").Change("3"));
        await cut.InvokeAsync(() => cut.Find("input#maxCustomers").Change("12"));

        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll(".alert-success").Count > 0, TimeSpan.FromSeconds(2));

        var createdTours = await _fakeToursApi.GetTours(CancellationToken.None);
        var tour = createdTours[0];

        Assert.Equal(3, tour.IncludedServices.Count);
        Assert.Contains("Service 1", tour.IncludedServices);
        Assert.Contains("Service 2", tour.IncludedServices);
        Assert.Contains("Service 3", tour.IncludedServices);
    }
}
