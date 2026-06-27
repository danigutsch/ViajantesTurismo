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
            Assert.True(
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
            Assert.Contains("We couldn't load the customer right now. Please try again.", alert.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Database unavailable", alert.TextContent, StringComparison.Ordinal);
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
            Assert.Contains("We couldn't update the customer right now. Please try again.", alert.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Customer update exploded", alert.TextContent, StringComparison.Ordinal);
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
            Assert.Equal(customer.PersonalInfo.FirstName, firstNameInput.GetAttribute("value"));

            var lastNameInput = cut.Find("input#lastName");
            Assert.Equal(customer.PersonalInfo.LastName, lastNameInput.GetAttribute("value"));

            var emailInput = cut.Find("input#email");
            Assert.Equal(customer.ContactInfo.Email, emailInput.GetAttribute("value"));
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
            Assert.NotNull(card.QuerySelector("input#firstName"));
            Assert.NotNull(card.QuerySelector("input#lastName"));
            Assert.NotNull(card.QuerySelector("input#birthDate"));
            Assert.NotNull(card.QuerySelector("input#gender"));
            Assert.NotNull(card.QuerySelector("#nationality"));
            Assert.NotNull(card.QuerySelector("input#occupation"));
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
            Assert.NotNull(card.QuerySelector("input#email"));
            Assert.NotNull(card.QuerySelector("input#mobile"));
            Assert.NotNull(card.QuerySelector("input#instagram"));
            Assert.NotNull(card.QuerySelector("input#facebook"));
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
            Assert.NotNull(card.QuerySelector("input#nationalId"));
            Assert.NotNull(card.QuerySelector("#idNationality"));
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
            Assert.Equal(3, options.Length);
            Assert.Contains("None", options[0].TextContent, StringComparison.Ordinal);
            Assert.Contains("Regular", options[1].TextContent, StringComparison.Ordinal);
            Assert.Contains("E-Bike", options[2].TextContent, StringComparison.Ordinal);
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
            Assert.NotNull(card.QuerySelector("select#roomType"));
            Assert.NotNull(card.QuerySelector("select#bedType"));
            Assert.Contains("Companion", card.TextContent, StringComparison.Ordinal);
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
            Assert.Equal(2, options.Length);
            Assert.Contains("Double Room", options[0].TextContent, StringComparison.Ordinal);
            Assert.Contains("Single Room", options[1].TextContent, StringComparison.Ordinal);
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
            Assert.Equal(2, options.Length);
            Assert.Contains("Single Bed", options[0].TextContent, StringComparison.Ordinal);
            Assert.Contains("Double Bed", options[1].TextContent, StringComparison.Ordinal);
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
            Assert.NotNull(card.QuerySelector("input#emergencyName"));
            Assert.NotNull(card.QuerySelector("input#emergencyMobile"));
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
            Assert.NotNull(allergiesTextArea);
            Assert.Equal("3", allergiesTextArea.GetAttribute("rows"));

            var additionalInfoTextArea = card.QuerySelector("textarea#additionalInfo");
            Assert.NotNull(additionalInfoTextArea);
            Assert.Equal("3", additionalInfoTextArea.GetAttribute("rows"));
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
            Assert.Contains("Customer updated successfully", cancelledAlert.TextContent, StringComparison.Ordinal);

            var goToDetailsButton = cancelledAlert.QuerySelector("button");
            Assert.NotNull(goToDetailsButton);
            Assert.Contains("Go to Details", goToDetailsButton.TextContent, StringComparison.Ordinal);
        });
    }
}
