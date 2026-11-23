using Microsoft.AspNetCore.Components;
using PandaBear.Testing;
using System.Net;
using PandaBear.HttpClients;
using ViajantesTurismo.Management.Web.Components.Pages.Customers;
using ViajantesTurismo.Management.WebTests.Infrastructure;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

[TestScope(TestScope.Component)]
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
        Services.AddSingleton<PandaBear.Blazor.IUiFailureReporter>(new RecordingUiFailureReporter());
    }

    [Fact]
    public void Changing_the_booking_history_owner_refreshes_the_customer_and_their_history()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose;
        var first = BuildCustomerDetailsDto();
        var secondName = first.PersonalInfo with
        {
            FirstName = "SecondHistoryCustomer"
        };
        var second = BuildCustomerDetailsDto(personalInfo: secondName);
        var firstBooking = BuildBookingDto(customerId: first.Id);
        var secondBooking = BuildBookingDto(companionId: second.Id);
        _fakeCustomersApi.AddCustomerDetails(first);
        _fakeCustomersApi.AddCustomerDetails(second);
        _fakeBookingsApi.AddBooking(firstBooking);
        _fakeBookingsApi.AddBooking(secondBooking);
        var cut = Render<Details>(parameters => parameters.Add(page => page.Id, first.Id));
        cut.WaitForElement($"a[href='/bookings/{firstBooking.Id}']");

        // Act
        cut.Render(parameters => parameters.Add(page => page.Id, second.Id));

        // Assert
        cut.WaitForElement($"a[href='/bookings/{secondBooking.Id}']");
        cut.Markup.ShouldContain(second.PersonalInfo.FirstName, StringComparison.Ordinal);
        cut.FindAll($"a[href='/bookings/{firstBooking.Id}']").ShouldBeEmpty();
    }

    [Fact]
    [Trait(TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public void Customer_details_defers_booking_choices_until_the_form_opens()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);
        foreach (var index in Enumerable.Range(0, 800))
        {
            _fakeCustomersApi.AddCustomer(BuildCustomerDto(firstName: $"Fixture{index:D3}"));
        }

        // Act
        var cut = Render<Details>(parameters => parameters.Add(page => page.Id, customer.Id));
        cut.WaitForElement("button:contains('Add Booking')");

        // Assert
        _fakeCustomersApi.LastGetCustomersMaxItems.ShouldBeNull();
        _fakeToursApi.GetToursCallCount.ShouldBe(0);
    }

    [Fact]
    [Trait(TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Booking_form_finds_a_tour_beyond_the_first_option_window_and_loads_its_details()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        _fakeCustomersApi.AddCustomerDetails(customer);
        foreach (var index in Enumerable.Range(0, 800))
        {
            _fakeToursApi.AddTour(BuildTourDto(
                id: Guid.Parse($"10000000-0000-0000-0000-{index:D12}"),
                identifier: $"FIXTURE-{index:D3}", name: $"Fixture Tour {index:D3}"));
        }

        var selected = BuildTourDto(
            id: Guid.Parse("00000000-0000-0000-0000-000000000001"),
            identifier: "TARGET-TOUR", name: "Selected Tour", maxCustomers: 12, currentCustomerCount: 5);
        _fakeToursApi.AddTour(selected);
        var detailIds = new List<Guid>();
        _fakeToursApi.SetGetTourByIdHandler((id, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            detailIds.Add(id);
            return Task.FromResult(HttpOutcomeMapper.Succeeded(HttpStatusCode.OK, selected));
        });
        var cut = Render<Details>(parameters => parameters.Add(page => page.Id, customer.Id));

        // Act
        await cut.WaitForElement("button:contains('Add Booking')").ClickAsync();
        var form = cut.FindComponent<CustomerBookingCreateForm>();
        form.WaitForAssertion(() => form.FindAll("#tourId option").Count.ShouldBe(21));
        var firstWindow = form.Find("#tourId").TextContent;
        await form.Find("#tourSearch").InputAsync(new ChangeEventArgs { Value = "target-tour" });
        await form.Find("#tourId").ChangeAsync(new ChangeEventArgs { Value = selected.Id.ToString() });

        // Assert
        firstWindow.ShouldNotContain(selected.Name, StringComparison.Ordinal);
        form.WaitForAssertion(() => form.Markup.ShouldContain("7 spots available", StringComparison.Ordinal));
        form.Markup.ShouldContain("Price Breakdown", StringComparison.Ordinal);
        detailIds.ShouldBe([selected.Id]);
        _fakeToursApi.GetToursCallCount.ShouldBe(0);
        _fakeCustomersApi.LastGetCustomersMaxItems.ShouldBeNull();
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
        heading.TextContent.ShouldContain("Customer Details", StringComparison.Ordinal);
        heading.InnerHtml.ShouldContain("bi-person-circle", StringComparison.Ordinal);
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
        backButton.TextContent.ShouldContain("Back to Customers", StringComparison.Ordinal);
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
        editButton.TextContent.ShouldContain("Edit Customer", StringComparison.Ordinal);
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
            BirthDate = DateOnly.FromDateTime(new DateTime(1990, 5, 15, 0, 0, 0, DateTimeKind.Unspecified)),
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
        card.TextContent.ShouldContain("Jane Smith", StringComparison.Ordinal);
        card.TextContent.ShouldContain("15/05/1990", StringComparison.Ordinal);
        card.TextContent.ShouldContain("Female", StringComparison.Ordinal);
        card.TextContent.ShouldContain("Canada", StringComparison.Ordinal);
        card.TextContent.ShouldContain("Software Developer", StringComparison.Ordinal);
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
        emailLink.TextContent.ShouldBe("test@example.com");

        var mobileLink = cut.Find("a[href='tel:+1-555-0123']");
        mobileLink.TextContent.ShouldBe("+1-555-0123");
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
        instagramLink.TextContent.ShouldBe("johndoe");
        instagramLink.GetAttribute("target").ShouldBe("_blank");
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
        html.ShouldNotContain("Instagram", StringComparison.Ordinal);
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
        facebookLink.TextContent.ShouldBe("john.doe");
        facebookLink.GetAttribute("target").ShouldBe("_blank");
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
        card.TextContent.ShouldContain("XYZ987654", StringComparison.Ordinal);
        card.TextContent.ShouldContain("Brazil", StringComparison.Ordinal);
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
            Country = "US"
        };
        var customer = BuildCustomerDetailsDto(address: address);
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var card = cut.Find("div.card:has(h5:contains('Address'))");
        card.TextContent.ShouldContain("456 Oak Avenue", StringComparison.Ordinal);
        card.TextContent.ShouldContain("Apt 7B", StringComparison.Ordinal);
        card.TextContent.ShouldContain("Westside", StringComparison.Ordinal);
        card.TextContent.ShouldContain("90210", StringComparison.Ordinal);
        card.TextContent.ShouldContain("Los Angeles", StringComparison.Ordinal);
        card.TextContent.ShouldContain("CA", StringComparison.Ordinal);
        card.TextContent.ShouldContain("United States", StringComparison.Ordinal);
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
            Country = "US"
        };
        var customer = BuildCustomerDetailsDto(address: address);
        _fakeCustomersApi.AddCustomerDetails(customer);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));
        // Assert
        var card = cut.Find("div.card:has(h5:contains('Address'))");
        var complementLabel = card.QuerySelector("dt:contains('Complement')");
        complementLabel.ShouldBeNull();
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
        card.TextContent.ShouldContain("82 kg", StringComparison.Ordinal);
        card.TextContent.ShouldContain("180 cm", StringComparison.Ordinal);
        card.TextContent.ShouldContain("EBike", StringComparison.Ordinal);
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
        card.TextContent.ShouldContain("Single", StringComparison.Ordinal);
        card.TextContent.ShouldContain("Double Bed", StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_companion_ID_when_present()
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
        card.TextContent.ShouldContain(companionId.ToString(), StringComparison.Ordinal);
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
        card.TextContent.ShouldContain("Sarah Connor", StringComparison.Ordinal);

        var mobileLink = cut.Find("a[href='tel:+1-555-HELP']");
        mobileLink.TextContent.ShouldBe("+1-555-HELP");
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
        card.TextContent.ShouldContain("Peanuts, Shellfish", StringComparison.Ordinal);
        card.TextContent.ShouldContain("Requires insulin", StringComparison.Ordinal);
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
        card.TextContent.ShouldContain("No medical information provided", StringComparison.Ordinal);
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
        _ = bookingsCard.ShouldNotBeNull();

        var addBookingButton = cut.Find("button:contains('Add Booking')");
        _ = addBookingButton.ShouldNotBeNull();
    }

    [Fact]
    [Trait(TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Create_booking_validation_problem_shows_server_validation_errors()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        var tour = BuildTourDto();
        _fakeCustomersApi.AddCustomerDetails(customer);
        _fakeCustomersApi.AddCustomer(BuildCustomerDto(id: customer.Id));
        _fakeToursApi.AddTour(tour);
        _fakeBookingsApi.SetCreateBookingOutcome(new HttpOutcome
        {
            Kind = HttpOutcomeKind.ValidationProblem,
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
        await cut.InvokeAsync(() => form.Find("#tourId").Change(tour.Id));
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
    [Trait(TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Create_booking_validation_problem_withholds_unknown_and_hostile_server_text()
    {
        // Arrange
        const string Hostile = "<script>alert(1)</script> at Server.Internal.Handler() in /src/Secret.cs:line 42";
        var customer = BuildCustomerDetailsDto();
        var tour = BuildTourDto();
        _fakeCustomersApi.AddCustomerDetails(customer);
        _fakeCustomersApi.AddCustomer(BuildCustomerDto(id: customer.Id));
        _fakeToursApi.AddTour(tour);
        _fakeBookingsApi.SetCreateBookingOutcome(new HttpOutcome
        {
            Kind = HttpOutcomeKind.ValidationProblem,
            StatusCode = HttpStatusCode.BadRequest,
            ValidationErrors = new Dictionary<string, string[]>
            {
                ["internalRuleSet"] = [Hostile],
                [nameof(CreateBookingDto.TourId)] = [Hostile]
            }
        });
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("button:contains('Add Booking')"));
        await cut.InvokeAsync(() => cut.Find("button:contains('Add Booking')").Click());
        await cut.WaitForAssertionAsync(() => cut.FindComponent<CustomerBookingCreateForm>());
        var form = cut.FindComponent<CustomerBookingCreateForm>();
        await cut.InvokeAsync(() => form.Find("#tourId").Change(tour.Id));
        form.Instance.Model.PrincipalBikeType = BikeTypeDto.Regular;

        // Act
        await cut.InvokeAsync(() => form.Find("form").Submit());

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            cut.Markup.ShouldContain("Failed to create booking. Please try again.", StringComparison.Ordinal);
            cut.Markup.ShouldContain("This value is not valid. Check it and try again.", StringComparison.Ordinal);
        });
        cut.Markup.ShouldNotContain("alert(1)", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("Secret.cs", StringComparison.Ordinal);
    }

    [Fact]
    [Trait(TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Create_booking_non_success_outcome_shows_sanitized_error()
    {
        // Arrange
        var customer = BuildCustomerDetailsDto();
        var tour = BuildTourDto();
        _fakeCustomersApi.AddCustomerDetails(customer);
        _fakeCustomersApi.AddCustomer(BuildCustomerDto(id: customer.Id));
        _fakeToursApi.AddTour(tour);
        _fakeBookingsApi.SetCreateBookingOutcome(
            HttpOutcomeMapper.Status(HttpOutcomeKind.Conflict, HttpStatusCode.Conflict));

        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("button:contains('Add Booking')"));

        // Act
        await cut.InvokeAsync(() => cut.Find("button:contains('Add Booking')").Click());
        await cut.WaitForAssertionAsync(() => cut.FindComponent<CustomerBookingCreateForm>());

        var form = cut.FindComponent<CustomerBookingCreateForm>();
        await cut.InvokeAsync(() => form.Find("#tourId").Change(tour.Id));
        form.Instance.Model.PrincipalBikeType = BikeTypeDto.Regular;
        await cut.InvokeAsync(() => form.Find("form").Submit());

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            cut.Markup.ShouldContain("Failed to create booking. Please try again.", StringComparison.Ordinal);
            cut.Markup.ShouldContain("Create New Booking", StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait(TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Create_booking_success_shows_confirmation_and_closes_form()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose;
        var customer = BuildCustomerDetailsDto();
        var tour = BuildTourDto();
        _fakeCustomersApi.AddCustomerDetails(customer);
        _fakeCustomersApi.AddCustomer(BuildCustomerDto(id: customer.Id));
        _fakeToursApi.AddTour(tour);

        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, customer.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("button:contains('Add Booking')"));

        // Act
        await cut.InvokeAsync(() => cut.Find("button:contains('Add Booking')").Click());
        await cut.WaitForAssertionAsync(() => cut.FindComponent<CustomerBookingCreateForm>());

        var form = cut.FindComponent<CustomerBookingCreateForm>();
        await cut.InvokeAsync(() => form.Find("#tourId").Change(tour.Id));
        form.Instance.Model.PrincipalBikeType = BikeTypeDto.Regular;
        await cut.InvokeAsync(() => form.Find("form").Submit());

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            cut.Markup.ShouldContain("Booking created successfully.", StringComparison.Ordinal);
            cut.Markup.ShouldNotContain("Create New Booking", StringComparison.Ordinal);
            cut.Markup.ShouldNotContain("No bookings found.", StringComparison.Ordinal);
        });
        _fakeBookingsApi.HistoryRequests.ShouldBe(Enumerable.Repeat((customer.Id, false, 1, BookingPageSort.Default, false), 2));
        _fakeBookingsApi.GetBookingHistoryCollectionCallCount.ShouldBe(0);
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
        _ = toast.ShouldNotBeNull();
    }

    [Fact]
    public void Transport_failure_while_loading_shows_a_safe_error_message()
    {
        // Arrange
        _fakeCustomersApi.SetGetCustomerByIdOutcome(FakeApiClientOutcomes.TransportFailure<CustomerDetailsDto>());

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, Guid.NewGuid()));

        // Assert
        cut.WaitForAssertion(() => cut.Find("[role='alert']").TextContent.ShouldBe("The customer could not be loaded."));
    }
}
