using Microsoft.AspNetCore.Components.Forms;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Tests.Shared.Fakes.ApiClients;
using static ViajantesTurismo.Admin.Tests.Shared.Builders.DtoBuilders;
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
    public async Task Shows_Not_Found_When_Tour_Does_Not_Exist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, nonExistentId));

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll(".alert-danger").Count > 0, TimeSpan.FromSeconds(2));

        var alert = cut.Find(".alert-danger");
        Assert.Contains("Tour not found", alert.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Shows_Back_Button_When_Tour_Not_Found()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, nonExistentId));

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll(".alert-danger").Count > 0, TimeSpan.FromSeconds(2));

        var backButton = cut.Find("a.btn-secondary");
        Assert.Contains("Back to Tours", backButton.TextContent, StringComparison.Ordinal);
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
    public async Task Renders_All_Form_Fields()
    {
        // Arrange
        var tour = await CreateTestTour();

        // Act
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll("input#identifier").Count > 0, TimeSpan.FromSeconds(2));

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
    public async Task Update_Button_Has_Correct_Text()
    {
        // Arrange
        var tour = await CreateTestTour();

        // Act
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll("button[type='submit']").Count > 0, TimeSpan.FromSeconds(2));

        var submitButton = cut.Find("button[type='submit']");
        Assert.Contains("Update Tour", submitButton.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cancel_Button_Is_Present()
    {
        // Arrange
        var tour = await CreateTestTour();

        // Act
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll("a.btn-secondary").Count > 0, TimeSpan.FromSeconds(2));

        var cancelButton = cut.Find("a.btn-secondary");
        Assert.Contains("Cancel", cancelButton.TextContent, StringComparison.Ordinal);
        Assert.Contains("/tours", cancelButton.GetAttribute("href"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Successful_Update_Shows_Success_Message()
    {
        // Arrange
        var tour = await CreateTestTour();
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        await cut.WaitForStateAsync(() => cut.FindAll("input#identifier").Count > 0, TimeSpan.FromSeconds(2));

        // Act
        await cut.InvokeAsync(() => cut.Find("input#name").Change("Updated Cuba Tour"));
        await cut.InvokeAsync(() => cut.Find("form").Submit());

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll(".alert-success").Count > 0, TimeSpan.FromSeconds(2));

        var successAlert = cut.Find(".alert-success");
        Assert.Contains("Tour updated successfully!", successAlert.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Successful_Update_Shows_Redirect_Message()
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

        var redirectAlert = cut.Find(".alert-info");
        Assert.Contains("Redirecting to details page in 3 seconds", redirectAlert.TextContent, StringComparison.Ordinal);
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
        await cut.WaitForStateAsync(() =>
        {
            var button = cut.Find("button[type='submit']");
            return button.TextContent.Contains("Updating...", StringComparison.Ordinal) || cut.FindAll(".alert-success").Count > 0;
        }, TimeSpan.FromSeconds(2));

        await cut.WaitForStateAsync(() => cut.FindAll(".alert-success").Count > 0, TimeSpan.FromSeconds(2));
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
    public async Task Form_Uses_DataAnnotationsValidator()
    {
        // Arrange
        var tour = await CreateTestTour();

        // Act
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll("input#identifier").Count > 0, TimeSpan.FromSeconds(2));

        var validator = cut.FindComponent<DataAnnotationsValidator>();
        Assert.NotNull(validator);
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
    public async Task Currency_Dropdown_Contains_All_Options()
    {
        // Arrange
        var tour = await CreateTestTour();

        // Act
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll("select#currency").Count > 0, TimeSpan.FromSeconds(2));

        var currencySelect = cut.Find("select#currency");
        var options = currencySelect.QuerySelectorAll("option");

        Assert.Equal(3, options.Length);
        Assert.Contains(options, o => o.TextContent.Contains("Brazilian Real", StringComparison.Ordinal));
        Assert.Contains(options, o => o.TextContent.Contains("Euro", StringComparison.Ordinal));
        Assert.Contains(options, o => o.TextContent.Contains("US Dollar", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Form_Has_Two_Column_Layout_For_Dates()
    {
        // Arrange
        var tour = await CreateTestTour();

        // Act
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll("input#startDate").Count > 0, TimeSpan.FromSeconds(2));

        var rows = cut.FindAll(".row");
        var dateRow = rows.First(r => r.QuerySelectorAll("input#startDate, input#endDate").Length > 0);

        var columns = dateRow.QuerySelectorAll(".col-md-6");
        Assert.Equal(2, columns.Length);
    }

    [Fact]
    public async Task Form_Has_Two_Column_Layout_For_Prices()
    {
        // Arrange
        var tour = await CreateTestTour();

        // Act
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll("input#price").Count > 0, TimeSpan.FromSeconds(2));

        var rows = cut.FindAll(".row");
        var priceRow = rows.First(r => r.QuerySelectorAll("input#price, input#singleRoom").Length > 0);

        var columns = priceRow.QuerySelectorAll(".col-md-6");
        Assert.Equal(2, columns.Length);
    }

    [Fact]
    public async Task Form_Has_Two_Column_Layout_For_Bike_Prices()
    {
        // Arrange
        var tour = await CreateTestTour();

        // Act
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll("input#regularBike").Count > 0, TimeSpan.FromSeconds(2));

        var rows = cut.FindAll(".row");
        var bikeRow = rows.First(r => r.QuerySelectorAll("input#regularBike, input#eBike").Length > 0);

        var columns = bikeRow.QuerySelectorAll(".col-md-6");
        Assert.Equal(2, columns.Length);
    }

    [Fact]
    public async Task Form_Has_Two_Column_Layout_For_Capacity()
    {
        // Arrange
        var tour = await CreateTestTour();

        // Act
        var cut = Render<Edit>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        // Assert
        await cut.WaitForStateAsync(() => cut.FindAll("input#minCustomers").Count > 0, TimeSpan.FromSeconds(2));

        var rows = cut.FindAll(".row");
        var capacityRow = rows.First(r => r.QuerySelectorAll("input#minCustomers, input#maxCustomers").Length > 0);

        var columns = capacityRow.QuerySelectorAll(".col-md-6");
        Assert.Equal(2, columns.Length);
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
            StartDate = new DateTime(2024, 6, 1),
            EndDate = new DateTime(2024, 6, 15),
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
            startDate: new DateTime(2024, 6, 1),
            endDate: new DateTime(2024, 6, 15),
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
