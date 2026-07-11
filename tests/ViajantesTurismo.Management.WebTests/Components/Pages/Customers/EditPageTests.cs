using Microsoft.AspNetCore.Components.Web;
using ViajantesTurismo.Management.Web.Components.Pages.Customers;
using ViajantesTurismo.Management.WebTests.Infrastructure;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

public class EditPageTests : BunitContext
{
    private readonly FakeCustomersApiClient _fakeCustomersApi = new();

    public EditPageTests()
    {
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
        Services.AddSingleton<ICountryService>(new FakeCountryService());
    }

    [Fact]
    public void Renders_loading_state()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customerId));

        // Assert
        cut.WaitForAssertion(() =>
            TestAssert.True(
                cut.Markup.Contains("Loading...", StringComparison.Ordinal)
                || cut.Markup.Contains("Customer not found.", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task OnInitializedAsync_when_load_fails_shows_sanitized_error_message()
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
            TestAssert.Contains("We couldn't load the customer right now. Please try again.", alert.TextContent, StringComparison.Ordinal);
            TestAssert.DoesNotContain("Database unavailable", alert.TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task HandleSubmit_when_update_fails_shows_sanitized_error_message()
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
            TestAssert.Contains("We couldn't update the customer right now. Please try again.", alert.TextContent, StringComparison.Ordinal);
            TestAssert.DoesNotContain("Customer update exploded", alert.TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Loads_and_displays_customer_data()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var firstNameInput = cut.Find("input#firstName");
            TestAssert.Equal(customer.PersonalInfo.FirstName, firstNameInput.GetAttribute("value"));

            var lastNameInput = cut.Find("input#lastName");
            TestAssert.Equal(customer.PersonalInfo.LastName, lastNameInput.GetAttribute("value"));

            var emailInput = cut.Find("input#email");
            TestAssert.Equal(customer.ContactInfo.Email, emailInput.GetAttribute("value"));
        });
    }

    [Fact]
    public async Task Renders_personal_information_card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Personal Information", StringComparison.Ordinal));
            TestAssert.NotNull(card.QuerySelector("input#firstName"));
            TestAssert.NotNull(card.QuerySelector("input#lastName"));
            TestAssert.NotNull(card.QuerySelector("input#birthDate"));
            TestAssert.NotNull(card.QuerySelector("input#gender"));
            TestAssert.NotNull(card.QuerySelector("#nationality"));
            TestAssert.NotNull(card.QuerySelector("input#occupation"));
        });
    }

    [Fact]
    public async Task Renders_contact_information_card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Contact Information", StringComparison.Ordinal));
            TestAssert.NotNull(card.QuerySelector("input#email"));
            TestAssert.NotNull(card.QuerySelector("input#mobile"));
            TestAssert.NotNull(card.QuerySelector("input#instagram"));
            TestAssert.NotNull(card.QuerySelector("input#facebook"));
        });
    }

    [Fact]
    public async Task Renders_identification_card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Identification", StringComparison.Ordinal));
            TestAssert.NotNull(card.QuerySelector("input#nationalId"));
            TestAssert.NotNull(card.QuerySelector("#idNationality"));
        });
    }

    [Fact]
    public async Task Renders_address_card_with_all_fields()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Address", StringComparison.Ordinal));
            TestAssert.NotNull(card.QuerySelector("input#street"));
            TestAssert.NotNull(card.QuerySelector("input#complement"));
            TestAssert.NotNull(card.QuerySelector("input#neighborhood"));
            TestAssert.NotNull(card.QuerySelector("input#postalCode"));
            TestAssert.NotNull(card.QuerySelector("input#city"));
            TestAssert.NotNull(card.QuerySelector("input#state"));
            TestAssert.NotNull(card.QuerySelector("input#country"));
        });
    }

    [Fact]
    public async Task Renders_physical_information_card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Physical Information", StringComparison.Ordinal));

            var weightInput = card.QuerySelector("input#weight");
            _ = TestAssert.NotNull(weightInput);
            TestAssert.Contains("Weight (kg)", card.TextContent, StringComparison.Ordinal);

            var heightInput = card.QuerySelector("input#height");
            _ = TestAssert.NotNull(heightInput);
            TestAssert.Contains("Height (cm)", card.TextContent, StringComparison.Ordinal);

            var bikeTypeSelect = card.QuerySelector("select#bikeType");
            _ = TestAssert.NotNull(bikeTypeSelect);
        });
    }

    [Fact]
    public async Task BikeType_dropdown_has_all_options()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var bikeTypeSelect = cut.Find("select#bikeType");
            var options = bikeTypeSelect.QuerySelectorAll("option");
            TestAssert.Equal(3, options.Length);
            TestAssert.Contains("None", options[0].TextContent, StringComparison.Ordinal);
            TestAssert.Contains("Regular", options[1].TextContent, StringComparison.Ordinal);
            TestAssert.Contains("E-Bike", options[2].TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Renders_accommodation_preferences_card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Accommodation Preferences", StringComparison.Ordinal));
            TestAssert.NotNull(card.QuerySelector("select#roomType"));
            TestAssert.NotNull(card.QuerySelector("select#bedType"));
            TestAssert.Contains("Companion", card.TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task RoomType_dropdown_has_all_options()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var roomTypeSelect = cut.Find("select#roomType");
            var options = roomTypeSelect.QuerySelectorAll("option");
            TestAssert.Equal(2, options.Length);
            TestAssert.Contains("Double Room", options[0].TextContent, StringComparison.Ordinal);
            TestAssert.Contains("Single Room", options[1].TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task BedType_dropdown_has_all_options()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var bedTypeSelect = cut.Find("select#bedType");
            var options = bedTypeSelect.QuerySelectorAll("option");
            TestAssert.Equal(2, options.Length);
            TestAssert.Contains("Single Bed", options[0].TextContent, StringComparison.Ordinal);
            TestAssert.Contains("Double Bed", options[1].TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Renders_emergency_contact_card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Emergency Contact", StringComparison.Ordinal));
            TestAssert.NotNull(card.QuerySelector("input#emergencyName"));
            TestAssert.NotNull(card.QuerySelector("input#emergencyMobile"));
        });
    }

    [Fact]
    public async Task Renders_medical_information_card()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.WaitForStateAsync(() => cut.Markup.Contains("Update Customer", StringComparison.Ordinal));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var card = cut.FindAll(".card").First(c => c.TextContent.Contains("Medical Information", StringComparison.Ordinal));

            var allergiesTextArea = card.QuerySelector("textarea#allergies");
            _ = TestAssert.NotNull(allergiesTextArea);
            TestAssert.Equal("3", allergiesTextArea.GetAttribute("rows"));

            var additionalInfoTextArea = card.QuerySelector("textarea#additionalInfo");
            _ = TestAssert.NotNull(additionalInfoTextArea);
            TestAssert.Equal("3", additionalInfoTextArea.GetAttribute("rows"));
        });
    }

    [Fact]
    public async Task Can_cancel_redirect_after_update()
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
            TestAssert.Contains("Customer updated successfully", cancelledAlert.TextContent, StringComparison.Ordinal);

            var goToDetailsButton = cancelledAlert.QuerySelector("button");
            _ = TestAssert.NotNull(goToDetailsButton);
            TestAssert.Contains("Go to Details", goToDetailsButton.TextContent, StringComparison.Ordinal);
        });
    }
}
