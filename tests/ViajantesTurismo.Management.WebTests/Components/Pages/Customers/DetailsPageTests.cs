using System.Net;
using SharedKernel.HttpClients;
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
        (heading.TextContent).ShouldContain("Customer Details", StringComparison.Ordinal);
        (heading.InnerHtml).ShouldContain("bi-person-circle", StringComparison.Ordinal);
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
        (backButton.TextContent).ShouldContain("Back to Customers", StringComparison.Ordinal);
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
        (editButton.TextContent).ShouldContain("Edit Customer", StringComparison.Ordinal);
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
        (card.TextContent).ShouldContain("Jane Smith", StringComparison.Ordinal);
        (card.TextContent).ShouldContain("15/05/1990", StringComparison.Ordinal);
        (card.TextContent).ShouldContain("Female", StringComparison.Ordinal);
        (card.TextContent).ShouldContain("Canada", StringComparison.Ordinal);
        (card.TextContent).ShouldContain("Software Developer", StringComparison.Ordinal);
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
        (emailLink.TextContent).ShouldBe("test@example.com");

        var mobileLink = cut.Find("a[href='tel:+1-555-0123']");
        (mobileLink.TextContent).ShouldBe("+1-555-0123");
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
        (instagramLink.TextContent).ShouldBe("johndoe");
        (instagramLink.GetAttribute("target")).ShouldBe("_blank");
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
        (html).ShouldNotContain("Instagram", StringComparison.Ordinal);
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
        (facebookLink.TextContent).ShouldBe("john.doe");
        (facebookLink.GetAttribute("target")).ShouldBe("_blank");
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
        (card.TextContent).ShouldContain("XYZ987654", StringComparison.Ordinal);
        (card.TextContent).ShouldContain("Brazil", StringComparison.Ordinal);
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
        (card.TextContent).ShouldContain("456 Oak Avenue", StringComparison.Ordinal);
        (card.TextContent).ShouldContain("Apt 7B", StringComparison.Ordinal);
        (card.TextContent).ShouldContain("Westside", StringComparison.Ordinal);
        (card.TextContent).ShouldContain("90210", StringComparison.Ordinal);
        (card.TextContent).ShouldContain("Los Angeles", StringComparison.Ordinal);
        (card.TextContent).ShouldContain("CA", StringComparison.Ordinal);
        (card.TextContent).ShouldContain("USA", StringComparison.Ordinal);
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
        (complementLabel).ShouldBeNull();
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
        (card.TextContent).ShouldContain("82 kg", StringComparison.Ordinal);
        (card.TextContent).ShouldContain("180 cm", StringComparison.Ordinal);
        (card.TextContent).ShouldContain("EBike", StringComparison.Ordinal);
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
        (card.TextContent).ShouldContain("Single", StringComparison.Ordinal);
        (card.TextContent).ShouldContain("Double Bed", StringComparison.Ordinal);
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
        (card.TextContent).ShouldContain(companionId.ToString(), StringComparison.Ordinal);
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
        (card.TextContent).ShouldContain("Sarah Connor", StringComparison.Ordinal);

        var mobileLink = cut.Find("a[href='tel:+1-555-HELP']");
        (mobileLink.TextContent).ShouldBe("+1-555-HELP");
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
        (card.TextContent).ShouldContain("Peanuts, Shellfish", StringComparison.Ordinal);
        (card.TextContent).ShouldContain("Requires insulin", StringComparison.Ordinal);
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
        (card.TextContent).ShouldContain("No medical information provided", StringComparison.Ordinal);
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
        _ = (bookingsCard).ShouldNotBeNull();

        var addBookingButton = cut.Find("button:contains('Add Booking')");
        _ = (addBookingButton).ShouldNotBeNull();
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Create_booking_validation_problem_shows_server_validation_errors()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        var tour = BuildTourDto();
        _fakeCustomersApi.AddCustomerDetails(customer);
        _fakeCustomersApi.AddCustomer(BuildCustomerDto(id: customer.Id));
        _fakeToursApi.AddTour(tour);
        _fakeBookingsApi.SetCreateBookingOutcome(new ContractCommandOutcomeDto
        {
            Kind = ContractCommandOutcomeKind.ValidationProblem,
            StatusCode = HttpStatusCode.BadRequest,
            ValidationErrors = new Dictionary<string, string[]>
            {
                [nameof(CreateBookingDto.TourId)] = ["Select an available tour."]
            }
        });

        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("button:contains('Add Booking')"));

        // Act
        await cut.InvokeAsync(() => cut.Find("button:contains('Add Booking')").Click());
        await cut.WaitForAssertionAsync(() => cut.FindComponent<CustomerBookingCreateForm>());

        var form = cut.FindComponent<CustomerBookingCreateForm>();
        form.Instance.Model.TourId = tour.Id;
        form.Instance.Model.PrincipalBikeType = BikeTypeDto.Regular;
        await cut.InvokeAsync(() => form.Find("form").Submit());

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            cut.Markup.ShouldContain("Select an available tour.", StringComparison.Ordinal);
            cut.Markup.ShouldContain("Create New Booking", StringComparison.Ordinal);
        });
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
        _ = (toast).ShouldNotBeNull();
    }
}
