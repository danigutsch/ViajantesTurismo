using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Tests.Shared;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers;
using ViajantesTurismo.Admin.Web.Components.Shared;
using static ViajantesTurismo.Admin.Tests.Shared.DtoBuilders;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers;

public sealed class DetailsPageTests : BunitContext
{
    private readonly FakeBookingsApiClient _fakeBookingsApi = new();
    private readonly FakeCustomersApiClient _fakeCustomersApi = new();
    private readonly FakeToursApiClient _fakeToursApi = new();

    public DetailsPageTests()
    {
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
        Services.AddSingleton<IBookingsApiClient>(_fakeBookingsApi);
        Services.AddSingleton<IToursApiClient>(_fakeToursApi);
    }

    [Fact]
    public void Renders_NotFound_When_Customer_Is_Null()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customerId));
        cut.WaitForAssertion(() => cut.Find(".alert.alert-danger"));

        // Assert
        var alert = cut.Find(".alert.alert-danger");
        Assert.Contains("Customer not found", alert.TextContent);

        var backLink = cut.Find("a.btn.btn-secondary");
        Assert.Equal("/customers", backLink.GetAttribute("href"));
    }

    [Fact]
    public void Displays_Page_Title_And_Header()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var heading = cut.Find("h1");
        Assert.Contains("Customer Details", heading.TextContent);
        Assert.Contains("bi-person-circle", heading.InnerHtml);
    }

    [Fact]
    public void Displays_Back_To_Customers_Button()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var backButton = cut.Find("a.btn-outline-secondary[href='/customers']");
        Assert.Contains("Back to Customers", backButton.TextContent);
    }

    [Fact]
    public void Displays_Edit_Customer_Button()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var editButton = cut.Find($"a.btn-primary[href='/customers/{customer.Id}/edit']");
        Assert.Contains("Edit Customer", editButton.TextContent);
    }

    [Fact]
    public void Displays_Personal_Information_Card()
    {
        // Arrange
        var personalInfo = new PersonalInfoDto
        {
            FirstName = "Jane",
            LastName = "Smith",
            Gender = "Female",
            BirthDate = new DateTime(1990, 5, 15),
            Nationality = "Canada",
            Occupation = "Software Developer"
        };
        var customer = BuildCustomerDetailsDto(personalInfo: personalInfo);
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var card = cut.Find("div.card:has(h5:contains('Personal Information'))");
        Assert.Contains("Jane Smith", card.TextContent);
        Assert.Contains("15/05/1990", card.TextContent);
        Assert.Contains("Female", card.TextContent);
        Assert.Contains("Canada", card.TextContent);
        Assert.Contains("Software Developer", card.TextContent);
    }

    [Fact]
    public void Displays_Contact_Information_Card()
    {
        // Arrange
        var contactInfo = new ContactInfoDto
        {
            Email = "test@example.com",
            Mobile = "+1-555-0123",
            Instagram = null,
            Facebook = null
        };
        var customer = BuildCustomerDetailsDto(contactInfo: contactInfo);
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var emailLink = cut.Find("a[href='mailto:test@example.com']");
        Assert.Equal("test@example.com", emailLink.TextContent);

        var mobileLink = cut.Find("a[href='tel:+1-555-0123']");
        Assert.Equal("+1-555-0123", mobileLink.TextContent);
    }

    [Fact]
    public void Displays_Instagram_When_Present()
    {
        // Arrange
        var contactInfo = new ContactInfoDto
        {
            Email = "test@example.com",
            Mobile = "+1234567890",
            Instagram = "johndoe",
            Facebook = null
        };
        var customer = BuildCustomerDetailsDto(contactInfo: contactInfo);
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var instagramLink = cut.Find("a[href='https://instagram.com/johndoe']");
        Assert.Equal("johndoe", instagramLink.TextContent);
        Assert.Equal("_blank", instagramLink.GetAttribute("target"));
    }

    [Fact]
    public void Does_Not_Display_Instagram_When_Empty()
    {
        // Arrange
        var contactInfo = new ContactInfoDto
        {
            Email = "test@example.com",
            Mobile = "+1234567890",
            Instagram = null,
            Facebook = null
        };
        var customer = BuildCustomerDetailsDto(contactInfo: contactInfo);
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var html = cut.Markup;
        Assert.DoesNotContain("Instagram", html);
    }

    [Fact]
    public void Displays_Facebook_When_Present()
    {
        // Arrange
        var contactInfo = new ContactInfoDto
        {
            Email = "test@example.com",
            Mobile = "+1234567890",
            Instagram = null,
            Facebook = "john.doe"
        };
        var customer = BuildCustomerDetailsDto(contactInfo: contactInfo);
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var facebookLink = cut.Find("a[href='https://facebook.com/john.doe']");
        Assert.Equal("john.doe", facebookLink.TextContent);
        Assert.Equal("_blank", facebookLink.GetAttribute("target"));
    }

    [Fact]
    public void Displays_Identification_Information()
    {
        // Arrange
        var identificationInfo = new IdentificationInfoDto
        {
            NationalId = "XYZ987654",
            IdNationality = "Brazil"
        };
        var customer = BuildCustomerDetailsDto(identificationInfo: identificationInfo);
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var card = cut.Find("div.card:has(h5:contains('Identification'))");
        Assert.Contains("XYZ987654", card.TextContent);
        Assert.Contains("Brazil", card.TextContent);
    }

    [Fact]
    public void Displays_Address_Information()
    {
        // Arrange
        var address = new AddressDto
        {
            Street = "456 Oak Avenue",
            Complement = "Apt 7B",
            Neighborhood = "Westside",
            PostalCode = "90210",
            City = "Los Angeles",
            State = "CA",
            Country = "USA"
        };
        var customer = BuildCustomerDetailsDto(address: address);
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var card = cut.Find("div.card:has(h5:contains('Address'))");
        Assert.Contains("456 Oak Avenue", card.TextContent);
        Assert.Contains("Apt 7B", card.TextContent);
        Assert.Contains("Westside", card.TextContent);
        Assert.Contains("90210", card.TextContent);
        Assert.Contains("Los Angeles", card.TextContent);
        Assert.Contains("CA", card.TextContent);
        Assert.Contains("USA", card.TextContent);
    }

    [Fact]
    public void Does_Not_Display_Address_Complement_When_Empty()
    {
        // Arrange
        var address = new AddressDto
        {
            Street = "123 Main St",
            Complement = null,
            Neighborhood = "Downtown",
            PostalCode = "10001",
            City = "New York",
            State = "NY",
            Country = "USA"
        };
        var customer = BuildCustomerDetailsDto(address: address);
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var card = cut.Find("div.card:has(h5:contains('Address'))");
        var complementLabel = card.QuerySelector("dt:contains('Complement')");
        Assert.Null(complementLabel);
    }

    [Fact]
    public void Displays_Physical_Information()
    {
        // Arrange
        var physicalInfo = new PhysicalInfoDto
        {
            WeightKg = 82,
            HeightCentimeters = 180,
            BikeType = BikeTypeDto.EBike
        };
        var customer = BuildCustomerDetailsDto(physicalInfo: physicalInfo);
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var card = cut.Find("div.card:has(h5:contains('Physical Information'))");
        Assert.Contains("82 kg", card.TextContent);
        Assert.Contains("180 cm", card.TextContent);
        Assert.Contains("EBike", card.TextContent);
    }

    [Fact]
    public void Displays_Accommodation_Preferences()
    {
        // Arrange
        var accommodationPreferences = new AccommodationPreferencesDto
        {
            RoomType = RoomTypeDto.SingleOccupancy,
            BedType = BedTypeDto.DoubleBed,
            CompanionId = null
        };
        var customer = BuildCustomerDetailsDto(accommodationPreferences: accommodationPreferences);
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var card = cut.Find("div.card:has(h5:contains('Accommodation Preferences'))");
        Assert.Contains("Single", card.TextContent);
        Assert.Contains("Double Bed", card.TextContent);
    }

    [Fact]
    public void Displays_Companion_Id_When_Present()
    {
        // Arrange
        var companionId = Guid.NewGuid();
        var accommodationPreferences = new AccommodationPreferencesDto
        {
            RoomType = RoomTypeDto.DoubleOccupancy,
            BedType = BedTypeDto.DoubleBed,
            CompanionId = companionId
        };
        var customer = BuildCustomerDetailsDto(accommodationPreferences: accommodationPreferences);
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var card = cut.Find("div.card:has(h5:contains('Accommodation Preferences'))");
        Assert.Contains(companionId.ToString(), card.TextContent);
    }

    [Fact]
    public void Displays_Emergency_Contact_Information()
    {
        // Arrange
        var emergencyContact = new EmergencyContactDto
        {
            Name = "Sarah Connor",
            Mobile = "+1-555-HELP"
        };
        var customer = BuildCustomerDetailsDto(emergencyContact: emergencyContact);
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var card = cut.Find("div.card:has(h5:contains('Emergency Contact'))");
        Assert.Contains("Sarah Connor", card.TextContent);

        var mobileLink = cut.Find("a[href='tel:+1-555-HELP']");
        Assert.Equal("+1-555-HELP", mobileLink.TextContent);
    }

    [Fact]
    public void Displays_Medical_Information_When_Present()
    {
        // Arrange
        var medicalInfo = new MedicalInfoDto
        {
            Allergies = "Peanuts, Shellfish",
            AdditionalInfo = "Requires insulin"
        };
        var customer = BuildCustomerDetailsDto(medicalInfo: medicalInfo);
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var card = cut.Find("div.card:has(h5:contains('Medical Information'))");
        Assert.Contains("Peanuts, Shellfish", card.TextContent);
        Assert.Contains("Requires insulin", card.TextContent);
    }

    [Fact]
    public void Displays_No_Medical_Info_Message_When_Empty()
    {
        // Arrange
        var medicalInfo = new MedicalInfoDto
        {
            Allergies = null,
            AdditionalInfo = null
        };
        var customer = BuildCustomerDetailsDto(medicalInfo: medicalInfo);
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var card = cut.Find("div.card:has(h5:contains('Medical Information'))");
        Assert.Contains("No medical information provided", card.TextContent);
    }

    [Fact]
    public void Displays_Bookings_Section_Header()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var bookingsCard = cut.Find("div.card:has(h5:contains('Bookings'))");
        Assert.NotNull(bookingsCard);

        var addBookingButton = cut.Find("button:contains('Add Booking')");
        Assert.NotNull(addBookingButton);
    }

    [Fact]
    public void Has_ToastNotification_Component()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var toast = cut.FindComponent<ToastNotification>();
        Assert.NotNull(toast);
    }
}
