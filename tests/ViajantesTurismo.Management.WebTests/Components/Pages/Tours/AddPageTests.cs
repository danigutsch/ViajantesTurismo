using SharedKernel.HttpClients;
using Add = ViajantesTurismo.Management.Web.Components.Pages.Tours.Add;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Tours;

public class AddPageTests : BunitContext
{
    private readonly FakeToursApiClient _fakeToursApi;

    public AddPageTests()
    {
        _fakeToursApi = new FakeToursApiClient();
        Services.AddSingleton<IToursApiClient>(_fakeToursApi);
    }

    [Fact]
    public void Renders_page_title()
    {
        // Arrange
        // Act
        var cut = Render<Add>();

        // Assert
        var title = cut.Find("h1");
        (title.TextContent).ShouldBe("Add New Tour");
    }

    [Fact]
    public void Renders_all_form_fields()
    {
        // Arrange
        // Act
        var cut = Render<Add>();

        // Assert
        (cut.Find("input#identifier")).ShouldNotBeNull();
        (cut.Find("input#name")).ShouldNotBeNull();
        (cut.Find("input#startDate")).ShouldNotBeNull();
        (cut.Find("input#endDate")).ShouldNotBeNull();
        (cut.Find("select#currency")).ShouldNotBeNull();
        (cut.Find("input#price")).ShouldNotBeNull();
        (cut.Find("input#singleRoom")).ShouldNotBeNull();
        (cut.Find("input#regularBike")).ShouldNotBeNull();
        (cut.Find("input#eBike")).ShouldNotBeNull();
        (cut.Find("textarea#services")).ShouldNotBeNull();
        (cut.Find("input#minCustomers")).ShouldNotBeNull();
        (cut.Find("input#maxCustomers")).ShouldNotBeNull();
    }

    [Fact]
    public void Currency_dropdown_contains_all_options()
    {
        // Arrange
        // Act
        var cut = Render<Add>();

        // Assert
        var currencySelect = cut.Find("select#currency");
        var options = currencySelect.QuerySelectorAll("option");

        (options.Length).ShouldBe(3);
        (options).ShouldContain(o => o.TextContent.Contains("Brazilian Real", StringComparison.Ordinal));
        (options).ShouldContain(o => o.TextContent.Contains("Euro", StringComparison.Ordinal));
        (options).ShouldContain(o => o.TextContent.Contains("US Dollar", StringComparison.Ordinal));
    }

    [Fact]
    public void Submit_button_has_correct_initial_text()
    {
        // Arrange
        // Act
        var cut = Render<Add>();

        // Assert
        var submitButton = cut.Find("button[type='submit']");
        (submitButton.TextContent).ShouldContain("Create Tour", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Shows_validation_summary()
    {
        // Arrange
        var cut = Render<Add>();

        // Act
        var form = cut.Find("form");
        await cut.InvokeAsync(() => form.Submit());

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll(".validation-message").Count > 0 || cut.FindAll("ul.validation-errors").Count > 0, TimeSpan.FromSeconds(2));

        var validationErrors = cut.FindAll("ul.validation-errors, .validation-message");
        (validationErrors).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Successful_submission_shows_success_message()
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
        (successAlert.TextContent).ShouldContain("Tour created successfully!", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Successful_submission_shows_action_buttons()
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

        (buttons.Length >= 3).ShouldBeTrue();
        (buttons).ShouldContain(b => b.TextContent.Contains("View Tour Details", StringComparison.Ordinal));
        (buttons).ShouldContain(b => b.TextContent.Contains("Create Another Tour", StringComparison.Ordinal));
        (buttons).ShouldContain(b => b.TextContent.Contains("View All Tours", StringComparison.Ordinal));
        buttons.ShouldContain(b => b.GetAttribute("href")?.StartsWith("/tours/", StringComparison.Ordinal) == true);
        buttons.ShouldNotContain(b => b.GetAttribute("href")?.StartsWith("/api/v1/", StringComparison.Ordinal) == true);
    }

    [Theory]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    [InlineData("/api/v1/route")]
    [InlineData("/api/v1/tours-malformed/relative-id")]
    public async Task Successful_submission_preserves_api_prefix_when_tours_path_prefix_does_not_match(
        string location)
    {
        // Arrange
        _fakeToursApi.SetCreateTourOutcome(ContractCommandOutcome.Succeeded(
            System.Net.HttpStatusCode.Created,
            new Uri(location, UriKind.Relative)));
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

        var detailsLink = cut.FindAll("a.btn")
            .First(link => link.TextContent.Contains("View Tour Details", StringComparison.Ordinal));
        detailsLink.GetAttribute("href").ShouldBe(location);
    }

    [Fact]
    public async Task Create_another_button_resets_form()
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
        (cut.FindAll(".alert-success")).ShouldBeEmpty();
    }

    [Fact]
    public async Task Submission_shows_spinner_and_disabled_button()
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
            (cut.Find("button[type='submit']").TextContent.Contains("Creating...", StringComparison.Ordinal)
                || cut.FindAll(".alert-success").Count > 0).ShouldBeTrue(),
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task API_error_shows_error_message()
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
        (errorAlert.TextContent).ShouldContain("We couldn't create the tour right now. Please try again.", StringComparison.Ordinal);
        (errorAlert.TextContent).ShouldNotContain("Failed to create tour", StringComparison.Ordinal);
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Validation_problem_outcome_shows_server_validation_errors()
    {
        // Arrange
        _fakeToursApi.SetCreateTourOutcome(new ContractCommandOutcomeDto
        {
            Kind = ContractCommandOutcomeKind.ValidationProblem,
            StatusCode = System.Net.HttpStatusCode.BadRequest,
            ValidationErrors = new Dictionary<string, string[]> { ["Name"] = ["Tour name already exists."] }
        });
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
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Tour name already exists.", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        var errorAlert = cut.Find(".alert-danger");
        errorAlert.TextContent.ShouldContain("Please correct the highlighted fields.", StringComparison.Ordinal);
    }

    [Fact]
    public void Form_uses_DataAnnotationsValidator()
    {
        // Arrange
        // Act
        var cut = Render<Add>();

        // Assert
        var validator = cut.FindComponent<DataAnnotationsValidator>();
        _ = (validator).ShouldNotBeNull();
    }

    [Fact]
    public void Single_room_supplement_field_is_present()
    {
        // Arrange
        // Act
        var cut = Render<Add>();

        // Assert
        var field = cut.Find("input#singleRoom");
        var label = cut.FindAll("label").First(l => l.GetAttribute("for") == "singleRoom");

        _ = (field).ShouldNotBeNull();
        (label.TextContent).ShouldContain("Single Room Supplement", StringComparison.Ordinal);
    }

    [Fact]
    public void Bike_price_fields_are_present()
    {
        // Arrange
        // Act
        var cut = Render<Add>();

        // Assert
        var regularBikeField = cut.Find("input#regularBike");
        var eBikeField = cut.Find("input#eBike");

        _ = (regularBikeField).ShouldNotBeNull();
        _ = (eBikeField).ShouldNotBeNull();

        var labels = cut.FindAll("label");
        (labels).ShouldContain(l => l.TextContent.Contains("Regular Bike Price", StringComparison.Ordinal));
        (labels).ShouldContain(l => l.TextContent.Contains("E-Bike Price", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Creates_tour_with_correct_data()
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
        (createdTours).ShouldHaveSingleItem();

        var tour = createdTours[0];
        (tour.Identifier).ShouldBe("CUBA2024");
        (tour.Name).ShouldBe("Cuba Adventure");
        (tour.Price).ShouldBe(1500.50m);
    }

    [Fact]
    public async Task Services_input_splits_by_lines()
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

        (tour.IncludedServices.Count).ShouldBe(3);
        (tour.IncludedServices).ShouldContain("Service 1");
        (tour.IncludedServices).ShouldContain("Service 2");
        (tour.IncludedServices).ShouldContain("Service 3");
    }
}
