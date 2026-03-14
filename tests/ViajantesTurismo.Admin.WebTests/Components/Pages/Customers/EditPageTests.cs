using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers;
using ViajantesTurismo.Admin.WebTests.Infrastructure;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers;

public class EditPageTests : BunitContext
{
    private readonly FakeCustomersApiClient _fakeCustomersApi = new();

    public EditPageTests()
    {
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
        var webHostEnvironment = new MockWebHostEnvironment();
        Services.AddSingleton<IWebHostEnvironment>(webHostEnvironment);
        Services.AddSingleton(new CountryService(webHostEnvironment));
    }

    [Fact]
    public void Renders_Loading_State()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customerId));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Loading", StringComparison.Ordinal) || cut.Markup.Contains("Customer not found", StringComparison.Ordinal), timeout: TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Shows_Not_Found_When_Customer_Does_Not_Exist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, nonExistentId));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var alert = cut.Find(".alert.alert-danger");
            Assert.Contains("Customer not found", alert.TextContent, StringComparison.Ordinal);
        });

        var backLink = cut.Find("a.btn.btn-secondary");
        Assert.Equal("/customers", backLink.GetAttribute("href"));
    }

    [Fact]
    public async Task OnInitializedAsync_When_Load_Fails_Shows_Sanitized_Error_Message()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        _fakeCustomersApi.SetGetCustomerByIdException(new InvalidOperationException("Database unavailable"));

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customerId));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var alert = cut.Find(".alert.alert-danger");
            Assert.Contains("We couldn't load the customer right now. Please try again.", alert.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Database unavailable", alert.TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task HandleSubmit_When_Update_Fails_Shows_Sanitized_Error_Message()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);
        _fakeCustomersApi.SetUpdateCustomerException(new InvalidOperationException("Customer update exploded"));

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Act
        var submitButton = cut.Find("button[type='submit']");
        await cut.InvokeAsync(async () => await submitButton.ClickAsync(new MouseEventArgs()));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var alert = cut.Find(".alert.alert-danger");
            Assert.Contains("We couldn't update the customer right now. Please try again.", alert.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Customer update exploded", alert.TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Loads_And_Displays_Customer_Data()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var firstNameInput = cut.Find("input#firstName");
            Assert.Equal(customer.PersonalInfo.FirstName, firstNameInput.GetAttribute("value"));

            var lastNameInput = cut.Find("input#lastName");
            Assert.Equal(customer.PersonalInfo.LastName, lastNameInput.GetAttribute("value"));

            var emailInput = cut.Find("input#email");
            Assert.Equal(customer.ContactInfo.Email, emailInput.GetAttribute("value"));
        });
    }

    [Fact]
    public async Task Renders_Personal_Information_Card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Personal Information", StringComparison.Ordinal));
            Assert.NotNull(card.QuerySelector("input#firstName"));
            Assert.NotNull(card.QuerySelector("input#lastName"));
            Assert.NotNull(card.QuerySelector("input#birthDate"));
            Assert.NotNull(card.QuerySelector("input#gender"));
            Assert.NotNull(card.QuerySelector("#nationality"));
            Assert.NotNull(card.QuerySelector("input#occupation"));
        });
    }

    [Fact]
    public async Task Renders_Contact_Information_Card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Contact Information", StringComparison.Ordinal));
            Assert.NotNull(card.QuerySelector("input#email"));
            Assert.NotNull(card.QuerySelector("input#mobile"));

            var instagramInput = card.QuerySelector("input#instagram");
            Assert.NotNull(instagramInput);
            Assert.Equal("username", instagramInput.GetAttribute("placeholder"));

            var facebookInput = card.QuerySelector("input#facebook");
            Assert.NotNull(facebookInput);
            Assert.Equal("facebook.com/username", facebookInput.GetAttribute("placeholder"));
        });
    }

    [Fact]
    public async Task Renders_Identification_Card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Identification", StringComparison.Ordinal));
            Assert.NotNull(card.QuerySelector("input#nationalId"));
            Assert.NotNull(card.QuerySelector("#idNationality"));
        });
    }

    [Fact]
    public async Task Renders_Address_Card_With_All_Fields()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Address", StringComparison.Ordinal));
            Assert.NotNull(card.QuerySelector("input#street"));
            Assert.NotNull(card.QuerySelector("input#complement"));
            Assert.NotNull(card.QuerySelector("input#neighborhood"));
            Assert.NotNull(card.QuerySelector("input#postalCode"));
            Assert.NotNull(card.QuerySelector("input#city"));
            Assert.NotNull(card.QuerySelector("input#state"));
            Assert.NotNull(card.QuerySelector("input#country"));
        });
    }

    [Fact]
    public async Task Renders_Physical_Information_Card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Physical Information", StringComparison.Ordinal));

            var weightInput = card.QuerySelector("input#weight");
            Assert.NotNull(weightInput);
            Assert.Contains("Weight (kg)", card.TextContent, StringComparison.Ordinal);

            var heightInput = card.QuerySelector("input#height");
            Assert.NotNull(heightInput);
            Assert.Contains("Height (cm)", card.TextContent, StringComparison.Ordinal);

            var bikeTypeSelect = card.QuerySelector("select#bikeType");
            Assert.NotNull(bikeTypeSelect);
        });
    }

    [Fact]
    public async Task BikeType_Dropdown_Has_All_Options()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var bikeTypeSelect = cut.Find("select#bikeType");
            var options = bikeTypeSelect.QuerySelectorAll("option");
            Assert.Equal(3, options.Length);
            Assert.Contains("None", options[0].TextContent, StringComparison.Ordinal);
            Assert.Contains("Regular", options[1].TextContent, StringComparison.Ordinal);
            Assert.Contains("E-Bike", options[2].TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Renders_Accommodation_Preferences_Card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Accommodation Preferences", StringComparison.Ordinal));
            Assert.NotNull(card.QuerySelector("select#roomType"));
            Assert.NotNull(card.QuerySelector("select#bedType"));
            Assert.Contains("Companion", card.TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task RoomType_Dropdown_Has_All_Options()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var roomTypeSelect = cut.Find("select#roomType");
            var options = roomTypeSelect.QuerySelectorAll("option");
            Assert.Equal(2, options.Length);
            Assert.Contains("Double Room", options[0].TextContent, StringComparison.Ordinal);
            Assert.Contains("Single Room", options[1].TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Does_Not_Render_Customer_Search_Input_In_Edit_Page()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() => { Assert.Empty(cut.FindAll("input[placeholder='Search customers by name or email...']")); });
    }

    [Fact]
    public async Task BedType_Dropdown_Has_All_Options()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var bedTypeSelect = cut.Find("select#bedType");
            var options = bedTypeSelect.QuerySelectorAll("option");
            Assert.Equal(2, options.Length);
            Assert.Contains("Single Bed", options[0].TextContent, StringComparison.Ordinal);
            Assert.Contains("Double Bed", options[1].TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Renders_Emergency_Contact_Card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Emergency Contact", StringComparison.Ordinal));
            Assert.NotNull(card.QuerySelector("input#emergencyName"));
            Assert.NotNull(card.QuerySelector("input#emergencyMobile"));
        });
    }

    [Fact]
    public async Task Renders_Medical_Information_Card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Medical Information", StringComparison.Ordinal));

            var allergiesTextArea = card.QuerySelector("textarea#allergies");
            Assert.NotNull(allergiesTextArea);
            Assert.Equal("3", allergiesTextArea.GetAttribute("rows"));

            var additionalInfoTextArea = card.QuerySelector("textarea#additionalInfo");
            Assert.NotNull(additionalInfoTextArea);
            Assert.Equal("3", additionalInfoTextArea.GetAttribute("rows"));
        });
    }

    [Fact]
    public async Task Renders_Update_Button()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var updateButton = cut.Find("button[type='submit']");
            Assert.Contains("Update Customer", updateButton.TextContent, StringComparison.Ordinal);
            Assert.Contains("btn-primary", updateButton.ClassName, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Renders_Cancel_Link()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var cancelLink = cut.Find("a:contains('Cancel')");
            Assert.Equal("/customers", cancelLink.GetAttribute("href"));
            Assert.Contains("btn-secondary", cancelLink.ClassName, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Renders_DataAnnotationsValidator()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var validator = cut.FindComponent<DataAnnotationsValidator>();
            Assert.NotNull(validator);
        });
    }

    [Fact]
    public async Task Renders_ValidationSummary()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var validationSummary = cut.FindComponent<ValidationSummary>();
            Assert.NotNull(validationSummary);
        });
    }

    [Fact]
    public async Task Shows_Success_Message_After_Successful_Update()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Act
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));
        var submitButton = cut.Find("button[type='submit']");
        await cut.InvokeAsync(async () => await submitButton.ClickAsync(new MouseEventArgs()));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var successAlert = cut.Find(".alert.alert-success");
            Assert.Contains("Customer updated successfully", successAlert.TextContent, StringComparison.Ordinal);
        }, timeout: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Shows_Pending_Redirect_Message_After_Update()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Act
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));
        var submitButton = cut.Find("button[type='submit']");
        await cut.InvokeAsync(async () => await submitButton.ClickAsync(new MouseEventArgs()));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var redirectAlert = cut.Find(".alert.alert-info");
            Assert.Contains("Redirecting to details page in 3 seconds", redirectAlert.TextContent, StringComparison.Ordinal);

            var cancelButton = redirectAlert.QuerySelector("button");
            Assert.Contains("Cancel", cancelButton!.TextContent, StringComparison.Ordinal);
        }, timeout: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Can_Cancel_Redirect_After_Update()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));
        var submitButton = cut.Find("button[type='submit']");
        await cut.InvokeAsync(async () => await submitButton.ClickAsync(new MouseEventArgs()));

        // Act
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Redirecting to details page", StringComparison.Ordinal));
        var cancelButton = cut.Find(".alert.alert-info button");
        await cut.InvokeAsync(async () => await cancelButton.ClickAsync(new MouseEventArgs()));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var alerts = cut.FindAll(".alert.alert-success");
            var cancelledAlert = alerts.Last(a => a.TextContent.Contains("Customer updated successfully", StringComparison.Ordinal));
            Assert.Contains("Customer updated successfully", cancelledAlert.TextContent, StringComparison.Ordinal);

            var goToDetailsButton = cancelledAlert.QuerySelector("button");
            Assert.NotNull(goToDetailsButton);
            Assert.Contains("Go to Details", goToDetailsButton.TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task All_Cards_Are_In_Proper_Layout()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var rows = cut.FindAll(".row");
            Assert.True(rows.Count >= 4); // At least 4 rows for card pairs

            // Verify cards are in two-column layout
            var firstRow = rows.First(r => r.TextContent.Contains("Personal Information", StringComparison.Ordinal));
            var columns = firstRow.QuerySelectorAll(".col-md-6");
            Assert.Equal(2, columns.Length);
        });
    }
}
