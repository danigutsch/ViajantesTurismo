using Edit = ViajantesTurismo.Admin.Web.Components.Pages.Tours.Edit;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Tours;

public class EditPageTests : BunitContext
{
    private readonly FakeToursApiClient _fakeToursApi;

    public EditPageTests()
    {
        _fakeToursApi = new FakeToursApiClient();
        Services.AddSingleton<IToursApiClient>(_fakeToursApi);
    }

    [Fact]
    public void Shows_Loading_State_Initially()
    {
        // Arrange
        var tourId = Guid.NewGuid();

        // Act
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tourId));

        // Assert
        var markup = cut.Markup;
        Assert.True(markup.Contains("Loading...", StringComparison.Ordinal) || markup.Contains("Tour not found", StringComparison.Ordinal) ||
                    markup.Contains("input#identifier", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Loads_Existing_Tour_Data()
    {
        // Arrange
        var tour = await CreateTestTour();

        // Act
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll("input#identifier").Count > 0, TimeSpan.FromSeconds(2));

        Assert.Equal("CUBA2024", cut.Find("input#identifier").GetAttribute("value"));
        Assert.Equal("Cuba Adventure", cut.Find("input#name").GetAttribute("value"));
    }

    [Fact]
    public async Task Renders_Page_Title()
    {
        // Arrange
        var tour = await CreateTestTour();

        // Act
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll("h1").Count > 0, TimeSpan.FromSeconds(2));

        var title = cut.Find("h1");
        Assert.Equal("Edit Tour", title.TextContent);
    }

    [Fact]
    public async Task Services_Are_Loaded_As_Multiline_Text()
    {
        // Arrange
        var tour = await CreateTestTour();

        // Act
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll("textarea#services").Count > 0, TimeSpan.FromSeconds(2));

        var markup = cut.Markup;

        Assert.Contains("Hotel", markup, StringComparison.Ordinal);
        Assert.Contains("Breakfast", markup, StringComparison.Ordinal);
        Assert.Contains("Lunch", markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cancel_Redirect_Button_Is_Present()
    {
        // Arrange
        var tour = await CreateTestTour();
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        await cut.WaitForStateAsync(() => cut.FindAll("input#identifier").Count > 0, TimeSpan.FromSeconds(2));

        // Act
        await cut.InvokeAsync(() => cut.Find("input#name").Change("Updated Tour"));
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll(".alert-info").Count > 0, TimeSpan.FromSeconds(2));

        var cancelButton = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel", StringComparison.Ordinal));
        Assert.NotNull(cancelButton);
    }

    [Fact]
    public async Task Cancel_Redirect_Shows_Alternative_Message()
    {
        // Arrange
        var tour = await CreateTestTour();
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        await cut.WaitForStateAsync(() => cut.FindAll("input#identifier").Count > 0, TimeSpan.FromSeconds(2));

        // Act
        await cut.InvokeAsync(() => cut.Find("input#name").Change("Updated Tour"));
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        await cut.WaitForStateAsync(() => cut.FindAll(".alert-info").Count > 0, TimeSpan.FromSeconds(2));

        var cancelButton = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel", StringComparison.Ordinal));
        await cut.InvokeAsync(() => cancelButton.Click());

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll(".alert-success").Count > 0 &&
                                          cut.FindAll(".alert-info").Count == 0,
            TimeSpan.FromSeconds(2));

        var successAlerts = cut.FindAll(".alert-success");
        Assert.True(successAlerts.Count >= 1, "Should have at least one success alert");

        var markup = cut.Markup;
        Assert.Contains("You can go to the details page now", markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task After_Cancel_Redirect_Shows_Go_To_Details_Button()
    {
        // Arrange
        var tour = await CreateTestTour();
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        await cut.WaitForStateAsync(() => cut.FindAll("input#identifier").Count > 0, TimeSpan.FromSeconds(2));

        // Act
        await cut.InvokeAsync(() => cut.Find("input#name").Change("Updated Tour"));
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        await cut.WaitForStateAsync(() => cut.FindAll(".alert-info").Count > 0, TimeSpan.FromSeconds(2));

        var cancelButton = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel", StringComparison.Ordinal));
        await cut.InvokeAsync(() => cancelButton.Click());

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll("button").Any(b => b.TextContent.Contains("Go to Details", StringComparison.Ordinal)),
            TimeSpan.FromSeconds(2));

        var goToDetailsButton = cut.FindAll("button").First(b => b.TextContent.Contains("Go to Details", StringComparison.Ordinal));
        Assert.NotNull(goToDetailsButton);
    }

    [Fact]
    public async Task API_Error_Shows_Error_Message()
    {
        // Arrange
        var tour = await CreateTestTour();
        _fakeToursApi.SetUpdateTourException(new InvalidOperationException("Failed to update tour"));

        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        await cut.WaitForStateAsync(() => cut.FindAll("input#identifier").Count > 0, TimeSpan.FromSeconds(2));

        // Act
        await cut.InvokeAsync(() => cut.Find("input#name").Change("Updated Tour"));
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll(".alert-danger").Count > 0, TimeSpan.FromSeconds(2));

        var errorAlert = cut.Find(".alert-danger");
        Assert.Contains("We couldn't update the tour right now. Please try again.", errorAlert.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Failed to update tour", errorAlert.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Submission_Shows_Spinner_And_Disabled_Button()
    {
        // Arrange
        var tour = await CreateTestTour();
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        await cut.WaitForStateAsync(() => cut.FindAll("input#identifier").Count > 0, TimeSpan.FromSeconds(2));

        // Act
        await cut.InvokeAsync(() => cut.Find("input#name").Change("Updated Tour"));
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert
        await cut.WaitForAssertionAsync(() =>
            Assert.True(
                cut.Find("button[type='submit']").TextContent.Contains("Updating...", StringComparison.Ordinal)
                || cut.FindAll(".alert-success").Count > 0),
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Updates_Tour_With_Modified_Data()
    {
        // Arrange
        var tour = await CreateTestTour();
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        await cut.WaitForStateAsync(() => cut.FindAll("input#identifier").Count > 0, TimeSpan.FromSeconds(2));

        // Act
        await cut.InvokeAsync(() => cut.Find("input#name").Change("Updated Cuba Adventure"));
        await cut.InvokeAsync(() => cut.Find("input#price").Change("1750.50"));
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll(".alert-success").Count > 0, TimeSpan.FromSeconds(2));

        var updatedTour = await _fakeToursApi.GetTourById(tour.Id, CancellationToken.None);
        Assert.NotNull(updatedTour);
        Assert.Equal("Updated Cuba Adventure", updatedTour.Name);
        Assert.Equal(1750.50m, updatedTour.Price);
    }

    [Fact]
    public async Task Load_Error_Shows_Error_Message()
    {
        // Arrange
        _fakeToursApi.SetGetTourByIdException(new InvalidOperationException("Database error"));
        var tourId = Guid.NewGuid();

        // Act
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tourId));

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll(".alert-danger").Count > 0, TimeSpan.FromSeconds(2));

        var errorAlert = cut.Find(".alert-danger");
        Assert.Contains("We couldn't load the tour right now. Please try again.", errorAlert.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Database error", errorAlert.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Identifier_Field_Is_Enabled_When_No_Bookings()
    {
        // Arrange
        var tour = await CreateTestTour();

        // Act
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll("input#identifier").Count > 0, TimeSpan.FromSeconds(2));

        var identifier = cut.Find("input#identifier");
        Assert.False(identifier.HasAttribute("disabled"));
    }

    [Fact]
    public async Task Identifier_Field_Is_Disabled_When_Bookings_Exist()
    {
        // Arrange
        var tour = CreateTestTourWithBookings();

        // Act
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll("input#identifier").Count > 0, TimeSpan.FromSeconds(2));

        var identifier = cut.Find("input#identifier");
        Assert.True(identifier.HasAttribute("disabled"));
    }

    [Fact]
    public async Task Currency_Field_Is_Enabled_When_No_Bookings()
    {
        // Arrange
        var tour = await CreateTestTour();

        // Act
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll("select#currency").Count > 0, TimeSpan.FromSeconds(2));

        var currency = cut.Find("select#currency");
        Assert.False(currency.HasAttribute("disabled"));
    }

    [Fact]
    public async Task Currency_Field_Is_Disabled_When_Bookings_Exist()
    {
        // Arrange
        var tour = CreateTestTourWithBookings();

        // Act
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll("select#currency").Count > 0, TimeSpan.FromSeconds(2));

        var currency = cut.Find("select#currency");
        Assert.True(currency.HasAttribute("disabled"));
    }

    private async Task<GetTourDto> CreateTestTour()
    {
        var createDto = new CreateTourDto
        {
            Identifier = "CUBA2024",
            Name = "Cuba Adventure",
            StartDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Unspecified),
            EndDate = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Unspecified),
            Currency = CurrencyDto.Euro,
            Price = 1500.00m,
            SingleRoomSupplementPrice = 200.00m,
            RegularBikePrice = 50.00m,
            EBikePrice = 100.00m,
            IncludedServices = ["Hotel", "Breakfast", "Lunch"],
            MinCustomers = 5,
            MaxCustomers = 15
        };

        await _fakeToursApi.CreateTour(createDto, CancellationToken.None);
        var tours = await _fakeToursApi.GetTours(CancellationToken.None);
        return tours[0];
    }

    private GetTourDto CreateTestTourWithBookings()
    {
        var tour = BuildTourDto(
            identifier: "CUBA2024",
            name: "Cuba Adventure",
            startDate: new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Unspecified),
            endDate: new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Unspecified),
            currency: CurrencyDto.Euro,
            price: 1500.00m,
            singleRoomSupplementPrice: 200.00m,
            regularBikePrice: 50.00m,
            eBikePrice: 100.00m,
            includedServices: new List<string> { "Hotel", "Breakfast", "Lunch" },
            minCustomers: 5,
            maxCustomers: 15,
            currentCustomerCount: 3);

        _fakeToursApi.AddTour(tour);
        return tour;
    }
}
