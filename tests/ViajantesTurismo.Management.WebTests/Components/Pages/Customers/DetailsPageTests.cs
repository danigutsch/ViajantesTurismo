using ViajantesTurismo.Management.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

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
    public void Displays_page_title_and_header()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var heading = cut.Find("h1");
        Assert.Contains("Customer Details", heading.TextContent, StringComparison.Ordinal);
        Assert.Contains("bi-person-circle", heading.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_back_to_customers_button()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var backButton = cut.Find("a.btn-outline-secondary[href='/customers']");
        Assert.Contains("Back to Customers", backButton.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_edit_customer_button()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var editButton = cut.Find($"a.btn-primary[href='/customers/{customer.Id}/edit']");
        Assert.Contains("Edit Customer", editButton.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_personal_information_card()
    {
        // Arrange
        var personalInfo = new PersonalInfoDto
        {
            FirstName = "Jane",
            LastName = "Smith",
            Gender = "Female",
            BirthDate = new DateTime(1990, 5, 15, 0, 0, 0, DateTimeKind.Unspecified),
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
        Assert.Contains("Jane Smith", card.TextContent, StringComparison.Ordinal);
        Assert.Contains("15/05/1990", card.TextContent, StringComparison.Ordinal);
        Assert.Contains("Female", card.TextContent, StringComparison.Ordinal);
        Assert.Contains("Canada", card.TextContent, StringComparison.Ordinal);
        Assert.Contains("Software Developer", card.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_contact_information_card()
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
    public void Displays_instagram_when_present()
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
    public void Does_not_display_instagram_when_empty()
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
        Assert.DoesNotContain("Instagram", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_facebook_when_present()
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
    public void Displays_identification_information()
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
        Assert.Contains("XYZ987654", card.TextContent, StringComparison.Ordinal);
        Assert.Contains("Brazil", card.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_address_information()
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
        Assert.Contains("456 Oak Avenue", card.TextContent, StringComparison.Ordinal);
        Assert.Contains("Apt 7B", card.TextContent, StringComparison.Ordinal);
        Assert.Contains("Westside", card.TextContent, StringComparison.Ordinal);
        Assert.Contains("90210", card.TextContent, StringComparison.Ordinal);
        Assert.Contains("Los Angeles", card.TextContent, StringComparison.Ordinal);
        Assert.Contains("CA", card.TextContent, StringComparison.Ordinal);
        Assert.Contains("USA", card.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Does_not_display_address_complement_when_empty()
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
    public void Displays_physical_information()
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
        Assert.Contains("82 kg", card.TextContent, StringComparison.Ordinal);
        Assert.Contains("180 cm", card.TextContent, StringComparison.Ordinal);
        Assert.Contains("EBike", card.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_accommodation_preferences()
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
        Assert.Contains("Single", card.TextContent, StringComparison.Ordinal);
        Assert.Contains("Double Bed", card.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_companion_id_when_present()
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
        Assert.Contains(companionId.ToString(), card.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_emergency_contact_information()
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
        Assert.Contains("Sarah Connor", card.TextContent, StringComparison.Ordinal);

        var mobileLink = cut.Find("a[href='tel:+1-555-HELP']");
        Assert.Equal("+1-555-HELP", mobileLink.TextContent);
    }

    [Fact]
    public void Displays_medical_information_when_present()
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
        Assert.Contains("Peanuts, Shellfish", card.TextContent, StringComparison.Ordinal);
        Assert.Contains("Requires insulin", card.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_no_medical_info_message_when_empty()
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
        Assert.Contains("No medical information provided", card.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_bookings_section_header()
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
    public void Has_toastnotification_component()
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
