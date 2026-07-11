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
            (cut.Markup.Contains("Loading...", StringComparison.Ordinal)
                || cut.Markup.Contains("Customer not found.", StringComparison.Ordinal)).ShouldBeTrue());
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
            (alert.TextContent).ShouldContain("We couldn't load the customer right now. Please try again.", StringComparison.Ordinal);
            (alert.TextContent).ShouldNotContain("Database unavailable", StringComparison.Ordinal);
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
            (alert.TextContent).ShouldContain("We couldn't update the customer right now. Please try again.", StringComparison.Ordinal);
            (alert.TextContent).ShouldNotContain("Customer update exploded", StringComparison.Ordinal);
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
            (firstNameInput.GetAttribute("value")).ShouldBe(customer.PersonalInfo.FirstName);

            var lastNameInput = cut.Find("input#lastName");
            (lastNameInput.GetAttribute("value")).ShouldBe(customer.PersonalInfo.LastName);

            var emailInput = cut.Find("input#email");
            (emailInput.GetAttribute("value")).ShouldBe(customer.ContactInfo.Email);
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
            (card.QuerySelector("input#firstName")).ShouldNotBeNull();
            (card.QuerySelector("input#lastName")).ShouldNotBeNull();
            (card.QuerySelector("input#birthDate")).ShouldNotBeNull();
            (card.QuerySelector("input#gender")).ShouldNotBeNull();
            (card.QuerySelector("#nationality")).ShouldNotBeNull();
            (card.QuerySelector("input#occupation")).ShouldNotBeNull();
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
            (card.QuerySelector("input#email")).ShouldNotBeNull();
            (card.QuerySelector("input#mobile")).ShouldNotBeNull();
            (card.QuerySelector("input#instagram")).ShouldNotBeNull();
            (card.QuerySelector("input#facebook")).ShouldNotBeNull();
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
            (card.QuerySelector("input#nationalId")).ShouldNotBeNull();
            (card.QuerySelector("#idNationality")).ShouldNotBeNull();
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
            (card.QuerySelector("input#street")).ShouldNotBeNull();
            (card.QuerySelector("input#complement")).ShouldNotBeNull();
            (card.QuerySelector("input#neighborhood")).ShouldNotBeNull();
            (card.QuerySelector("input#postalCode")).ShouldNotBeNull();
            (card.QuerySelector("input#city")).ShouldNotBeNull();
            (card.QuerySelector("input#state")).ShouldNotBeNull();
            (card.QuerySelector("input#country")).ShouldNotBeNull();
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
            _ = (weightInput).ShouldNotBeNull();
            (card.TextContent).ShouldContain("Weight (kg)", StringComparison.Ordinal);

            var heightInput = card.QuerySelector("input#height");
            _ = (heightInput).ShouldNotBeNull();
            (card.TextContent).ShouldContain("Height (cm)", StringComparison.Ordinal);

            var bikeTypeSelect = card.QuerySelector("select#bikeType");
            _ = (bikeTypeSelect).ShouldNotBeNull();
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
            (options.Length).ShouldBe(3);
            (options[0].TextContent).ShouldContain("None", StringComparison.Ordinal);
            (options[1].TextContent).ShouldContain("Regular", StringComparison.Ordinal);
            (options[2].TextContent).ShouldContain("E-Bike", StringComparison.Ordinal);
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
            (card.QuerySelector("select#roomType")).ShouldNotBeNull();
            (card.QuerySelector("select#bedType")).ShouldNotBeNull();
            (card.TextContent).ShouldContain("Companion", StringComparison.Ordinal);
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
            (options.Length).ShouldBe(2);
            (options[0].TextContent).ShouldContain("Double Room", StringComparison.Ordinal);
            (options[1].TextContent).ShouldContain("Single Room", StringComparison.Ordinal);
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
            (options.Length).ShouldBe(2);
            (options[0].TextContent).ShouldContain("Single Bed", StringComparison.Ordinal);
            (options[1].TextContent).ShouldContain("Double Bed", StringComparison.Ordinal);
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
            (card.QuerySelector("input#emergencyName")).ShouldNotBeNull();
            (card.QuerySelector("input#emergencyMobile")).ShouldNotBeNull();
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
            _ = (allergiesTextArea).ShouldNotBeNull();
            (allergiesTextArea.GetAttribute("rows")).ShouldBe("3");

            var additionalInfoTextArea = card.QuerySelector("textarea#additionalInfo");
            _ = (additionalInfoTextArea).ShouldNotBeNull();
            (additionalInfoTextArea.GetAttribute("rows")).ShouldBe("3");
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
            (cancelledAlert.TextContent).ShouldContain("Customer updated successfully", StringComparison.Ordinal);

            var goToDetailsButton = cancelledAlert.QuerySelector("button");
            _ = (goToDetailsButton).ShouldNotBeNull();
            (goToDetailsButton.TextContent).ShouldContain("Go to Details", StringComparison.Ordinal);
        });
    }
}
