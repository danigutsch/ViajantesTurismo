using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SharedKernel.HttpClients;
using ViajantesTurismo.Management.Web.Components.Pages.Bookings;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Bookings;

public sealed class EditPageTests : BunitContext
{
    private const string HeadingOrAlertSelector = "h1, .alert";
    private const string ValueAttributeName = "value";
    private const string DisabledAttributeName = "disabled";
    private const string ButtonSelector = "button";
    private const string CancelBookingText = "Cancel Booking";
    private const string DeleteBookingText = "Delete Booking";
    private const string VisibleModalSelector = ".modal.show";

    private readonly FakeBookingsApiClient _fakeBookingsApi = new();
    private readonly FakeCustomersApiClient _fakeCustomersApi = new();

    public EditPageTests()
    {
        Services.AddSingleton<IBookingsApiClient>(_fakeBookingsApi);
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
    }

    [Fact]
    public void Page_renders_successfully_with_valid_id()
    {
        // Arrange
        var booking = BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find(HeadingOrAlertSelector));
        // Assert
        var heading = cut.Find("h1");
        (heading.TextContent.Trim()).ShouldBe("Edit Booking");
    }

    [Fact]
    public void OnInitializedAsync_when_load_fails_shows_sanitized_error_message()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        _fakeBookingsApi.SetGetBookingByIdException(new InvalidOperationException("SQL timeout"));

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, bookingId));
        cut.WaitForAssertion(() => cut.Find(".alert.alert-danger"));

        // Assert
        var alert = cut.Find(".alert.alert-danger");
        (alert.TextContent).ShouldContain("We couldn't load the booking right now. Please try again.", StringComparison.Ordinal);
        (alert.TextContent).ShouldNotContain("SQL timeout", StringComparison.Ordinal);
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public void Customer_options_failure_keeps_booking_edit_available_to_an_operator()
    {
        // Arrange
        var authorization = AddAuthorization();
        authorization.SetAuthorized("operator@example.test", AuthorizationState.Authorized);
        authorization.SetRoles("Operator");
        var companionId = Guid.NewGuid();
        var booking = BuildBookingDto(
            companionId: companionId,
            companionName: "Existing Companion",
            companionBikeType: BikeTypeDto.Regular);
        _fakeBookingsApi.AddBooking(booking);
        _fakeCustomersApi.SetGetCustomersException(
            new HttpRequestException("customer.read forbidden", null, HttpStatusCode.Forbidden));

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("form"));

        // Assert
        cut.Find("#companionId").GetAttribute(ValueAttributeName).ShouldBe(companionId.ToString());
        cut.Markup.ShouldContain("Other companion choices are unavailable", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("customer.read forbidden", StringComparison.Ordinal);
        cut.FindAll(".alert.alert-danger").ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleSubmit_when_update_fails_shows_sanitized_error_message()
    {
        // Arrange
        var booking = BuildBookingDto(notes: "Original notes");
        _fakeBookingsApi.AddBooking(booking);
        _fakeBookingsApi.SetUpdateBookingNotesException(new InvalidOperationException("Update failed hard"));

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("form"));

        // Act
        var notesTextarea = cut.Find("#notes");
        await cut.InvokeAsync(() => notesTextarea.Change("Updated notes"));

        var form = cut.Find("form");
        await cut.InvokeAsync(async () => await form.SubmitAsync());

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var alert = cut.Find(".alert.alert-danger");
            (alert.TextContent).ShouldContain("We couldn't update the booking right now. Please try again.", StringComparison.Ordinal);
            (alert.TextContent).ShouldNotContain("Update failed hard", StringComparison.Ordinal);

            cut.Find("form");
            var retainedNotes = cut.Find("#notes");
            var retainedValue = retainedNotes.GetAttribute(ValueAttributeName) ?? retainedNotes.TextContent;
            retainedValue.ShouldBe("Updated notes");
        });
    }

    [Fact]
    public void Displays_booking_information_when_found()
    {
        // Arrange
        var booking = BuildBookingDto(
            tourName: "Portugal Adventure",
            tourIdentifier: "PT-2024-001",
            customerName: "John Doe",
            totalPrice: 2500m
        );
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find(HeadingOrAlertSelector));
        // Assert
        var tourLink = cut.Find($"a[href='/tours/{booking.TourId}']");
        (tourLink.TextContent).ShouldContain("Portugal Adventure", StringComparison.Ordinal);

        var tourIdentifier = cut.Find("small.text-muted");
        (tourIdentifier.TextContent).ShouldContain("PT-2024-001", StringComparison.Ordinal);

        var customerLink = cut.Find($"a[href='/customers/{booking.CustomerId}']");
        (customerLink.TextContent).ShouldContain("John Doe", StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_companion_information_when_present()
    {
        // Arrange
        var companionId = Guid.NewGuid();
        var booking = BuildBookingDto(
            companionId: companionId,
            companionName: "Jane Smith"
        );
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find(HeadingOrAlertSelector));
        // Assert
        var companionLink = cut.Find($"a[href='/customers/{companionId}']");
        (companionLink.TextContent).ShouldContain("Jane Smith", StringComparison.Ordinal);
    }

    [Fact]
    public void Does_not_display_companion_when_not_present()
    {
        // Arrange
        var booking = BuildBookingDto(companionId: null);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find(HeadingOrAlertSelector));
        // Assert
        var html = cut.Markup;
        (html).ShouldNotContain("Companion:", StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_booking_date_in_correct_format()
    {
        // Arrange
        var bookingDate = new DateTime(2024, 3, 15, 14, 30, 0, DateTimeKind.Utc);
        var booking = BuildBookingDto(bookingDate: bookingDate);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find(HeadingOrAlertSelector));
        // Assert
        var html = cut.Markup;
        (html).ShouldContain("15/03/2024 14:30", StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_total_price_as_readonly()
    {
        // Arrange
        var booking = BuildBookingDto(totalPrice: 3250.50m);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find(HeadingOrAlertSelector));
        // Assert
        var priceInput = cut.Find("#totalPrice");
        (priceInput.GetAttribute(ValueAttributeName)).ShouldBe("R$ 3,250.50");
        (priceInput.HasAttribute("readonly")).ShouldBeTrue();
    }

    [Fact]
    public void Displays_status_as_a_read_only_badge()
    {
        // Arrange
        var booking = BuildBookingDto(status: BookingStatusDto.Pending);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find(HeadingOrAlertSelector));
        // Assert
        cut.FindAll("#status").ShouldBeEmpty();
        var badge = cut.FindComponent<BookingStatusBadge>();
        badge.Instance.Status.ShouldBe(BookingStatusDto.Pending);
    }

    [Fact]
    public void Displays_payment_status_as_a_read_only_badge()
    {
        // Arrange
        var booking = BuildBookingDto(paymentStatus: PaymentStatusDto.Unpaid);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find(HeadingOrAlertSelector));
        // Assert
        cut.FindAll("#paymentStatus").ShouldBeEmpty();
        var badge = cut.FindComponent<PaymentStatusBadge>();
        badge.Instance.Status.ShouldBe(PaymentStatusDto.Unpaid);
    }

    [Fact]
    public void Preloads_existing_notes_in_form()
    {
        // Arrange
        var booking = BuildBookingDto(notes: "Customer requested early check-in");
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find(HeadingOrAlertSelector));
        // Assert
        var notesTextarea = cut.Find("#notes");
        var value = notesTextarea.GetAttribute(ValueAttributeName) ?? notesTextarea.TextContent;
        (value).ShouldContain("Customer requested early check-in", StringComparison.Ordinal);
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Preloads_room_bikes_and_editable_companion_choices()
    {
        // Arrange
        var principal = BuildCustomerDto(firstName: "Alice", lastName: "Principal", bikeType: BikeTypeDto.EBike);
        var companion = BuildCustomerDto(firstName: "Bob", lastName: "Companion", bikeType: BikeTypeDto.Regular);
        var alternative = BuildCustomerDto(firstName: "Carol", lastName: "Alternative", bikeType: BikeTypeDto.EBike);
        _fakeCustomersApi.AddCustomer(principal);
        _fakeCustomersApi.AddCustomer(companion);
        _fakeCustomersApi.AddCustomer(alternative);
        var booking = BuildBookingDto(
            customerId: principal.Id,
            companionId: companion.Id,
            companionName: "Bob Companion",
            roomType: RoomTypeDto.DoubleOccupancy,
            principalBikeType: BikeTypeDto.EBike,
            companionBikeType: BikeTypeDto.Regular);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("#roomType"));

        // Assert
        cut.Find("label[for='roomType']").TextContent.ShouldContain("Room Type", StringComparison.Ordinal);
        cut.Find("#roomType").GetAttribute(ValueAttributeName).ShouldBe(nameof(RoomTypeDto.DoubleOccupancy));
        cut.Find("label[for='principalBikeType']").TextContent.ShouldContain("Principal Bike", StringComparison.Ordinal);
        cut.Find("#principalBikeType").GetAttribute(ValueAttributeName).ShouldBe(nameof(BikeTypeDto.EBike));

        var companionSelect = cut.Find("#companionId");
        var companionOptions = companionSelect.QuerySelectorAll("option");
        companionSelect.GetAttribute(ValueAttributeName).ShouldBe(companion.Id.ToString());
        companionOptions.ShouldNotContain(option => option.TextContent.Contains("Alice Principal", StringComparison.Ordinal));
        companionOptions.ShouldContain(option => option.TextContent.Contains("Bob Companion", StringComparison.Ordinal));
        companionOptions.ShouldContain(option => option.TextContent.Contains("Carol Alternative", StringComparison.Ordinal));

        cut.Find("label[for='companionBikeType']").TextContent.ShouldContain("Companion Bike", StringComparison.Ordinal);
        cut.Find("#companionBikeType").GetAttribute(ValueAttributeName).ShouldBe(nameof(BikeTypeDto.Regular));
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public void Preloads_single_room_instead_of_the_form_default()
    {
        // Arrange
        var booking = BuildBookingDto(
            roomType: RoomTypeDto.SingleOccupancy,
            principalBikeType: BikeTypeDto.EBike);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("#roomType"));

        // Assert
        cut.Find("#roomType").GetAttribute(ValueAttributeName).ShouldBe(nameof(RoomTypeDto.SingleOccupancy));
        cut.FindAll("#companionId").ShouldBeEmpty();
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public void Companion_choices_are_not_truncated_after_one_hundred_customers()
    {
        // Arrange
        for (var index = 0; index < 100; index++)
        {
            _fakeCustomersApi.AddCustomer(BuildCustomerDto(firstName: $"Filler{index}"));
        }

        var companion = BuildCustomerDto(firstName: "Existing", lastName: "Companion");
        var alternative = BuildCustomerDto(firstName: "Later", lastName: "Alternative");
        _fakeCustomersApi.AddCustomer(companion);
        _fakeCustomersApi.AddCustomer(alternative);
        var booking = BuildBookingDto(
            companionId: companion.Id,
            companionName: "Existing Companion",
            companionBikeType: companion.BikeType);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("#companionId"));

        // Assert
        var options = cut.Find("#companionId").QuerySelectorAll("option");
        options.ShouldContain(option => option.GetAttribute(ValueAttributeName) == companion.Id.ToString());
        options.ShouldContain(option => option.GetAttribute(ValueAttributeName) == alternative.Id.ToString());
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Selecting_single_room_removes_companion_controls()
    {
        // Arrange
        var principal = BuildCustomerDto(firstName: "Alice", lastName: "Principal");
        var companion = BuildCustomerDto(firstName: "Bob", lastName: "Companion");
        _fakeCustomersApi.AddCustomer(principal);
        _fakeCustomersApi.AddCustomer(companion);
        var booking = BuildBookingDto(
            customerId: principal.Id,
            companionId: companion.Id,
            companionName: "Bob Companion",
            roomType: RoomTypeDto.DoubleOccupancy,
            companionBikeType: BikeTypeDto.Regular);
        _fakeBookingsApi.AddBooking(booking);
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("#roomType"));

        // Act
        await cut.Find("#roomType").ChangeAsync(new ChangeEventArgs { Value = nameof(RoomTypeDto.SingleOccupancy) });

        // Assert
        cut.FindAll("#companionId").ShouldBeEmpty();
        cut.FindAll("#companionBikeType").ShouldBeEmpty();
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Changed_details_are_updated_and_refreshed_before_redirect()
    {
        // Arrange
        var principal = BuildCustomerDto(firstName: "Alice", lastName: "Principal");
        var companion = BuildCustomerDto(firstName: "Bob", lastName: "Companion");
        _fakeCustomersApi.AddCustomer(principal);
        _fakeCustomersApi.AddCustomer(companion);
        var booking = BuildBookingDto(
            customerId: principal.Id,
            companionId: companion.Id,
            companionName: "Bob Companion",
            companionBikeType: BikeTypeDto.Regular,
            roomType: RoomTypeDto.DoubleOccupancy,
            principalBikeType: BikeTypeDto.Regular,
            totalPrice: 1000m);
        _fakeBookingsApi.AddBooking(booking);
        _fakeBookingsApi.SetUpdatedDetailsTotalPrice(1400m);
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("#roomType"));
        await cut.Find("#roomType").ChangeAsync(new ChangeEventArgs { Value = nameof(RoomTypeDto.SingleOccupancy) });
        await cut.Find("#principalBikeType").ChangeAsync(new ChangeEventArgs { Value = nameof(BikeTypeDto.EBike) });

        // Act
        var submitTask = cut.InvokeAsync(async () => await cut.Find("form").SubmitAsync());
        await cut.WaitForAssertionAsync(() => cut.FindAll(".alert.alert-info").ShouldContain(alert => alert.TextContent.Contains("Redirecting", StringComparison.Ordinal)));

        // Assert
        var updatedDetails = _fakeBookingsApi.LastUpdatedDetails;
        _ = updatedDetails.ShouldNotBeNull();
        updatedDetails.RoomType.ShouldBe(RoomTypeDto.SingleOccupancy);
        updatedDetails.PrincipalBikeType.ShouldBe(BikeTypeDto.EBike);
        updatedDetails.CompanionCustomerId.ShouldBeNull();
        updatedDetails.CompanionBikeType.ShouldBeNull();
        _fakeBookingsApi.GetBookingByIdCallCount.ShouldBe(2);
        cut.Find("#totalPrice").GetAttribute(ValueAttributeName).ShouldBe("R$ 1,400.00");
        cut.Find("#roomType").GetAttribute(ValueAttributeName).ShouldBe(nameof(RoomTypeDto.SingleOccupancy));

        var redirectAlert = cut.FindAll(".alert.alert-info").ShouldHaveSingleItem(alert => alert.TextContent.Contains("Redirecting", StringComparison.Ordinal));
        var cancelButton = redirectAlert.QuerySelector(ButtonSelector);
        _ = cancelButton.ShouldNotBeNull();
        await cut.InvokeAsync(() => cancelButton.Click());
        await submitTask;
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Details_only_update_does_not_write_an_unchanged_default_discount()
    {
        // Arrange
        var booking = BuildBookingDto(
            discountType: DiscountTypeDto.None,
            discountReason: null,
            principalBikeType: BikeTypeDto.Regular);
        _fakeBookingsApi.AddBooking(booking);
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("#principalBikeType"));
        await cut.Find("#principalBikeType").ChangeAsync(new ChangeEventArgs { Value = nameof(BikeTypeDto.EBike) });

        // Act
        var submitTask = cut.Find("form").SubmitAsync();
        await cut.WaitForAssertionAsync(() => cut.FindAll(".alert.alert-info").ShouldContain(
            alert => alert.TextContent.Contains("Redirecting", StringComparison.Ordinal)));

        // Assert
        _fakeBookingsApi.UpdateBookingDetailsCallCount.ShouldBe(1);
        _fakeBookingsApi.UpdateBookingDiscountCallCount.ShouldBe(0);
        var redirectAlert = cut.FindAll(".alert.alert-info")
            .ShouldHaveSingleItem(alert => alert.TextContent.Contains("Redirecting", StringComparison.Ordinal));
        var cancelButton = redirectAlert.QuerySelector(ButtonSelector);
        _ = cancelButton.ShouldNotBeNull();
        await cut.InvokeAsync(() => cancelButton.Click());
        await submitTask;
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Successful_update_with_refresh_failure_is_not_reported_as_a_failed_update()
    {
        // Arrange
        var booking = BuildBookingDto(notes: "Original notes");
        _fakeBookingsApi.AddBooking(booking);
        _fakeBookingsApi.SetGetBookingByIdExceptionOnCall(2, new InvalidOperationException("refresh failed hard"));
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("form"));
        await cut.Find("#notes").ChangeAsync(new ChangeEventArgs { Value = "Updated notes" });

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            cut.Markup.ShouldContain("Booking updated successfully!", StringComparison.Ordinal);
            cut.Markup.ShouldContain("The change was saved, but we couldn't refresh the booking", StringComparison.Ordinal);
            cut.Markup.ShouldNotContain("We couldn't update the booking", StringComparison.Ordinal);
            cut.Markup.ShouldNotContain("refresh failed hard", StringComparison.Ordinal);
            cut.Find("#notes").GetAttribute(ValueAttributeName).ShouldBe("Updated notes");
            cut.Find("button[type='submit']").HasAttribute(DisabledAttributeName).ShouldBeTrue();
        });
        _fakeBookingsApi.UpdateBookingNotesCallCount.ShouldBe(1);
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Partial_save_failure_reloads_authoritative_state_without_overwriting_entered_values()
    {
        // Arrange
        var booking = BuildBookingDto(notes: "Original notes", totalPrice: 1000m);
        _fakeBookingsApi.AddBooking(booking);
        _fakeBookingsApi.SetUpdatedDetailsTotalPrice(1300m);
        _fakeBookingsApi.SetUpdateBookingNotesException(new InvalidOperationException("notes failed hard"));
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("form"));
        await cut.Find("#principalBikeType").ChangeAsync(new ChangeEventArgs { Value = nameof(BikeTypeDto.EBike) });
        await cut.Find("#notes").ChangeAsync(new ChangeEventArgs { Value = "Attempted notes" });

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        _fakeBookingsApi.UpdateBookingDetailsCallCount.ShouldBe(1);
        _fakeBookingsApi.UpdateBookingNotesCallCount.ShouldBe(1);
        _fakeBookingsApi.GetBookingByIdCallCount.ShouldBe(2);
        cut.Find("#totalPrice").GetAttribute(ValueAttributeName).ShouldBe("R$ 1,300.00");
        cut.Find("#notes").GetAttribute(ValueAttributeName).ShouldBe("Attempted notes");
        cut.Markup.ShouldContain("We couldn't update the booking", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("notes failed hard", StringComparison.Ordinal);
        cut.Find("button[type='submit']").HasAttribute(DisabledAttributeName).ShouldBeFalse();
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Partial_save_with_failed_readback_locks_further_mutations()
    {
        // Arrange
        var booking = BuildBookingDto(notes: "Original notes");
        _fakeBookingsApi.AddBooking(booking);
        _fakeBookingsApi.SetUpdateBookingNotesException(new InvalidOperationException("notes failed hard"));
        _fakeBookingsApi.SetGetBookingByIdExceptionOnCall(2, new InvalidOperationException("readback failed hard"));
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("form"));
        await cut.Find("#principalBikeType").ChangeAsync(new ChangeEventArgs { Value = nameof(BikeTypeDto.EBike) });
        await cut.Find("#notes").ChangeAsync(new ChangeEventArgs { Value = "Attempted notes" });

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Some changes may have been saved, but we couldn't refresh the booking", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("readback failed hard", StringComparison.Ordinal);
        cut.Find("button[type='submit']").HasAttribute(DisabledAttributeName).ShouldBeTrue();
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Refresh_failure_disables_an_already_open_payment_form()
    {
        // Arrange
        var booking = BuildBookingDto(notes: "Original notes", remainingBalance: 250m);
        _fakeBookingsApi.AddBooking(booking);
        _fakeBookingsApi.SetGetBookingByIdExceptionOnCall(2, new InvalidOperationException("refresh failed hard"));
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("form"));
        var showPaymentButton = cut.FindAll(ButtonSelector)
            .ShouldHaveSingleItem(button => button.TextContent.Contains("Record Payment", StringComparison.Ordinal));
        await showPaymentButton.ClickAsync(new MouseEventArgs());
        var paymentForm = cut.FindComponent<PaymentForm>();
        paymentForm.Instance.Model.Amount = 100m;
        paymentForm.Instance.Model.Method = PaymentMethodDto.Cash;
        await cut.Find("#notes").ChangeAsync(new ChangeEventArgs { Value = "Updated notes" });

        // Act
        var updateTask = cut.FindAll("form")[0].SubmitAsync();
        await cut.WaitForAssertionAsync(() =>
            cut.Markup.ShouldContain("The change was saved, but we couldn't refresh the booking", StringComparison.Ordinal));
        paymentForm = cut.FindComponent<PaymentForm>();
        var paymentWasDisabled = paymentForm.Find("button[type='submit']").HasAttribute(DisabledAttributeName);
        await paymentForm.Find("form").SubmitAsync();

        // Assert
        await cut.FindAll(ButtonSelector)
            .ShouldHaveSingleItem(button => button.TextContent.Contains("Cancel", StringComparison.Ordinal) &&
                                            button.Closest(".alert.alert-info") is not null)
            .ClickAsync(new MouseEventArgs());
        await updateTask;
        paymentWasDisabled.ShouldBeTrue();
        _fakeBookingsApi.RecordPaymentCallCount.ShouldBe(0);
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Payment_success_with_refresh_failure_is_acknowledged_and_locks_further_mutations()
    {
        // Arrange
        var booking = BuildBookingDto(status: BookingStatusDto.Pending, remainingBalance: 250m);
        _fakeBookingsApi.AddBooking(booking);
        _fakeBookingsApi.SetGetBookingByIdExceptionOnCall(2, new InvalidOperationException("refresh failed hard"));
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find(HeadingOrAlertSelector));
        var showPaymentButton = cut.FindAll(ButtonSelector)
            .ShouldHaveSingleItem(button => button.TextContent.Contains("Record Payment", StringComparison.Ordinal));
        await showPaymentButton.ClickAsync(new MouseEventArgs());
        var paymentForm = cut.FindComponent<PaymentForm>();
        paymentForm.Instance.Model.Amount = 100m;
        paymentForm.Instance.Model.Method = PaymentMethodDto.Cash;

        // Act
        await paymentForm.Find("form").SubmitAsync();

        // Assert
        _fakeBookingsApi.RecordPaymentCallCount.ShouldBe(1);
        cut.Markup.ShouldContain("Payment recorded successfully.", StringComparison.Ordinal);
        cut.Markup.ShouldContain("The change was saved, but we couldn't refresh the booking", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("We couldn't record the payment", StringComparison.Ordinal);
        var actionButtons = cut.FindAll(".card.border-danger button");
        actionButtons.ShouldNotBeEmpty();
        actionButtons.ShouldAllSatisfy(button => button.HasAttribute(DisabledAttributeName).ShouldBeTrue());
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Payment_committed_before_transport_failure_locks_further_mutations()
    {
        // Arrange
        var booking = BuildBookingDto(status: BookingStatusDto.Pending, remainingBalance: 250m);
        _fakeBookingsApi.AddBooking(booking);
        _fakeBookingsApi.SetRecordPaymentExceptionAfterCommit(new HttpRequestException("response lost after commit"));
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find(HeadingOrAlertSelector));
        var showPaymentButton = cut.FindAll(ButtonSelector)
            .ShouldHaveSingleItem(button => button.TextContent.Contains("Record Payment", StringComparison.Ordinal));
        await showPaymentButton.ClickAsync(new MouseEventArgs());
        var paymentForm = cut.FindComponent<PaymentForm>();
        paymentForm.Instance.Model.Amount = 100m;
        paymentForm.Instance.Model.Method = PaymentMethodDto.Cash;

        // Act
        await paymentForm.Find("form").SubmitAsync();

        // Assert
        _fakeBookingsApi.RecordPaymentCallCount.ShouldBe(1);
        _fakeBookingsApi.CommittedPaymentCount.ShouldBe(1);
        cut.Markup.ShouldContain("The payment may have been recorded, but we couldn't confirm the result", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("response lost after commit", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("We couldn't record the payment right now", StringComparison.Ordinal);
        cut.Find("button[type='submit']").HasAttribute(DisabledAttributeName).ShouldBeTrue();
        var actionButtons = cut.FindAll(".card.border-danger button");
        actionButtons.ShouldNotBeEmpty();
        actionButtons.ShouldAllSatisfy(button => button.HasAttribute(DisabledAttributeName).ShouldBeTrue());
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Concurrent_payment_submits_dispatch_only_one_request()
    {
        // Arrange
        var booking = BuildBookingDto(status: BookingStatusDto.Pending, remainingBalance: 250m);
        _fakeBookingsApi.AddBooking(booking);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var paymentGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var paymentGateCancellation = cancellationToken.Register(() => paymentGate.TrySetCanceled(cancellationToken));
        _fakeBookingsApi.SetRecordPaymentTask(paymentGate.Task);
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find(HeadingOrAlertSelector));
        var showPaymentButton = cut.FindAll(ButtonSelector)
            .ShouldHaveSingleItem(button => button.TextContent.Contains("Record Payment", StringComparison.Ordinal));
        await showPaymentButton.ClickAsync(new MouseEventArgs());
        var paymentForm = cut.FindComponent<PaymentForm>();
        paymentForm.Instance.Model.Amount = 100m;
        paymentForm.Instance.Model.Method = PaymentMethodDto.Cash;

        // Act
        var firstPaymentTask = paymentForm.Find("form").SubmitAsync();
        await cut.WaitForStateAsync(() => _fakeBookingsApi.RecordPaymentCallCount == 1);
        paymentForm = cut.FindComponent<PaymentForm>();
        await paymentForm.Find("form").SubmitAsync();
        paymentGate.SetResult();
        await firstPaymentTask;

        // Assert
        _fakeBookingsApi.RecordPaymentCallCount.ShouldBe(1);
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Payment_in_flight_blocks_edit_and_lifecycle_requests()
    {
        // Arrange
        var booking = BuildBookingDto(status: BookingStatusDto.Pending, notes: "Original notes", remainingBalance: 250m);
        _fakeBookingsApi.AddBooking(booking);
        _fakeBookingsApi.SetUpdateBookingNotesException(new InvalidOperationException("notes should not run"));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var paymentGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var paymentGateCancellation = cancellationToken.Register(() => paymentGate.TrySetCanceled(cancellationToken));
        _fakeBookingsApi.SetRecordPaymentTask(paymentGate.Task);
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find(HeadingOrAlertSelector));
        var showPaymentButton = cut.FindAll(ButtonSelector)
            .ShouldHaveSingleItem(button => button.TextContent.Contains("Record Payment", StringComparison.Ordinal));
        await showPaymentButton.ClickAsync(new MouseEventArgs());
        var paymentForm = cut.FindComponent<PaymentForm>();
        paymentForm.Instance.Model.Amount = 100m;
        paymentForm.Instance.Model.Method = PaymentMethodDto.Cash;
        var paymentTask = paymentForm.Find("form").SubmitAsync();
        await cut.WaitForStateAsync(() => _fakeBookingsApi.RecordPaymentCallCount == 1);
        await cut.Find("#notes").ChangeAsync(new ChangeEventArgs { Value = "Attempted notes" });

        // Act
        await cut.InvokeAsync(() => cut.FindComponent<BookingEditActionPanel>().Instance.Confirm.InvokeAsync());
        await cut.FindAll("form")[0].SubmitAsync();
        paymentGate.SetResult();
        await paymentTask;

        // Assert
        _fakeBookingsApi.RecordPaymentCallCount.ShouldBe(1);
        _fakeBookingsApi.BookingActionCallCount.ShouldBe(0);
        _fakeBookingsApi.UpdateBookingNotesCallCount.ShouldBe(0);
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Unchanged_details_do_not_call_details_update()
    {
        // Arrange
        var booking = BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("form"));

        // Act
        var submitTask = cut.InvokeAsync(async () => await cut.Find("form").SubmitAsync());
        await cut.WaitForAssertionAsync(() => cut.FindAll(".alert.alert-info").ShouldContain(alert => alert.TextContent.Contains("Redirecting", StringComparison.Ordinal)));

        // Assert
        _fakeBookingsApi.LastUpdatedDetails.ShouldBeNull();

        var redirectAlert = cut.FindAll(".alert.alert-info").ShouldHaveSingleItem(alert => alert.TextContent.Contains("Redirecting", StringComparison.Ordinal));
        var cancelButton = redirectAlert.QuerySelector(ButtonSelector);
        _ = cancelButton.ShouldNotBeNull();
        await cut.InvokeAsync(() => cancelButton.Click());
        await submitTask;
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Companion_requires_a_bike_selection()
    {
        // Arrange
        var principal = BuildCustomerDto(firstName: "Alice", lastName: "Principal");
        var companion = BuildCustomerDto(firstName: "Bob", lastName: "Companion");
        _fakeCustomersApi.AddCustomer(principal);
        _fakeCustomersApi.AddCustomer(companion);
        var booking = BuildBookingDto(customerId: principal.Id);
        _fakeBookingsApi.AddBooking(booking);
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("#companionId"));
        await cut.Find("#companionId").ChangeAsync(new ChangeEventArgs { Value = companion.Id.ToString() });
        await cut.WaitForAssertionAsync(() => cut.Find("#companionBikeType"));
        await cut.Find("#companionBikeType").ChangeAsync(new ChangeEventArgs { Value = string.Empty });

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Companion bike type is required when a companion is selected.", StringComparison.Ordinal);
        _fakeBookingsApi.LastUpdatedDetails.ShouldBeNull();
    }

    [Fact]
    public void Renders_discount_type_dropdown_with_all_options()
    {
        // Arrange
        var booking = BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find(HeadingOrAlertSelector));
        // Assert
        var discountTypeSelect = cut.Find("#discountType");
        var options = discountTypeSelect.QuerySelectorAll("option");
        (options.Length).ShouldBe(3);

        (options).ShouldContain(o => o.TextContent.Contains("No Discount", StringComparison.Ordinal));
        (options).ShouldContain(o => o.TextContent.Contains("Percentage", StringComparison.Ordinal));
        (options).ShouldContain(o => o.TextContent.Contains("Absolute Amount", StringComparison.Ordinal));
    }

    [Fact]
    public void Does_not_show_discount_fields_when_type_is_none()
    {
        // Arrange
        var booking = BuildBookingDto(discountType: DiscountTypeDto.None);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find(HeadingOrAlertSelector));
        // Assert
        var html = cut.Markup;
        (html).ShouldNotContain("discountAmount", StringComparison.Ordinal);
        (html).ShouldNotContain("discountReason", StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_discount_amount_field_when_percentage_selected()
    {
        // Arrange
        var booking = BuildBookingDto(
            discountType: DiscountTypeDto.Percentage,
            discountAmount: 15m,
            discountReason: "Early bird discount"
        );
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var label = cut.Find("label[for='discountAmount']");
        (label.TextContent).ShouldContain("Discount Percentage", StringComparison.Ordinal);

        var formText = cut.Find("#discountAmount + .form-text");
        (formText.TextContent).ShouldContain("between 0 and 100", StringComparison.Ordinal);

        var discountReason = cut.Find("#discountReason");
        var reasonValue = discountReason.GetAttribute(ValueAttributeName) ?? discountReason.TextContent;
        (reasonValue).ShouldContain("Early bird discount", StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_discount_amount_field_when_absolute_selected()
    {
        // Arrange
        var booking = BuildBookingDto(
            discountType: DiscountTypeDto.Absolute,
            discountAmount: 250m,
            discountReason: "Group discount"
        );
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find(HeadingOrAlertSelector));
        // Assert
        var label = cut.Find("label[for='discountAmount']");
        (label.TextContent).ShouldContain("Discount Amount", StringComparison.Ordinal);
        (label.TextContent).ShouldNotContain("Percentage", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_cancel_button_with_link_to_bookings()
    {
        // Arrange
        var booking = BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find(HeadingOrAlertSelector));
        // Assert
        var cancelLink = cut.Find("a.btn.btn-secondary");
        (cancelLink.GetAttribute("href")).ShouldBe("/bookings");
        (cancelLink.TextContent).ShouldContain("Cancel", StringComparison.Ordinal);
    }

    [Fact]
    public void Submit_button_shows_default_text_initially()
    {
        // Arrange
        var booking = BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var submitButton = cut.Find("button[type='submit']");
        (submitButton.TextContent).ShouldContain("Update Booking", StringComparison.Ordinal);
        (submitButton.HasAttribute(DisabledAttributeName)).ShouldBeFalse();
    }

    [Theory]
    [InlineData(BookingStatusDto.Completed)]
    [InlineData(BookingStatusDto.Cancelled)]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public void Completed_and_cancelled_bookings_keep_edit_controls_disabled(BookingStatusDto status)
    {
        // Arrange
        var booking = BuildBookingDto(status: status);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("form"));

        // Assert
        cut.Find("#roomType").HasAttribute(DisabledAttributeName).ShouldBeTrue();
        cut.Find("#principalBikeType").HasAttribute(DisabledAttributeName).ShouldBeTrue();
        cut.Find("#notes").HasAttribute(DisabledAttributeName).ShouldBeTrue();
        cut.Find("#discountType").HasAttribute(DisabledAttributeName).ShouldBeTrue();
        cut.Find("button[type='submit']").HasAttribute(DisabledAttributeName).ShouldBeTrue();
    }

    [Fact]
    public async Task Cancel_redirect_button_shows_success_message()
    {
        // Arrange
        var booking = BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));

        // Act - Submit and then cancel redirect
        var form = cut.Find("form");
        await cut.InvokeAsync(async () => await form.SubmitAsync());

        var redirectAlert = cut.FindAll(".alert.alert-info").First(a => a.TextContent.Contains("Redirecting", StringComparison.Ordinal));
        var cancelButton = redirectAlert.QuerySelector(ButtonSelector);
        _ = (cancelButton).ShouldNotBeNull();
        await cut.InvokeAsync(() => cancelButton.Click());

        // Assert
        var successAlert = cut.Find(".alert.alert-success");
        (successAlert.TextContent).ShouldContain("Booking updated successfully!", StringComparison.Ordinal);

        var goToBookingsButton = cut.Find($".alert.alert-success {ButtonSelector}");
        (goToBookingsButton.TextContent).ShouldContain("Go to Bookings", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Buttons_are_disabled_during_submission()
    {
        // Arrange
        var booking = BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));

        // Act - Start submission
        var form = cut.Find("form");
        var submitTask = cut.InvokeAsync(async () => await form.SubmitAsync());

        // Assert - During submission
        var submitButton = cut.Find("button[type='submit']");
        (submitButton.HasAttribute(DisabledAttributeName)).ShouldBeTrue();
        (submitButton.TextContent).ShouldContain("Updating...", StringComparison.Ordinal);

        var spinner = submitButton.QuerySelector(".spinner-border");
        _ = (spinner).ShouldNotBeNull();

        var cancelLink = cut.Find("a.btn.btn-secondary");
        (cancelLink.GetAttribute("class")).ShouldContain(DisabledAttributeName, StringComparison.Ordinal);

        await submitTask;
    }

    [Fact]
    public void Displays_discount_reason_help_text()
    {
        // Arrange
        var booking = BuildBookingDto(
            discountType: DiscountTypeDto.Percentage,
            discountAmount: 10m,
            discountReason: "Test"
        );
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find(HeadingOrAlertSelector));
        // Assert
        var helpText = cut.Find("#discountReason + .form-text");
        (helpText.TextContent).ShouldContain("Required for audit purposes", StringComparison.Ordinal);
        (helpText.TextContent).ShouldContain("10-500 characters", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Record_payment_button_toggles_payment_form_visibility()
    {
        // Arrange
        var booking = BuildBookingDto(status: BookingStatusDto.Pending, remainingBalance: 250m);
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find(HeadingOrAlertSelector));

        // Act
        var showButton = cut.FindAll(ButtonSelector)
            .First(button => button.TextContent.Contains("Record Payment", StringComparison.Ordinal));
        await cut.InvokeAsync(() => showButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            (cut.Markup).ShouldContain("Record New Payment", StringComparison.Ordinal);
            (cut.FindAll("#amount")).ShouldNotBeEmpty();
        });

        // Act
        var hideButton = cut.FindAll(ButtonSelector)
            .First(button => button.TextContent.Trim() == "Cancel");
        await cut.InvokeAsync(() => hideButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            (cut.Markup).ShouldNotContain("Record New Payment", StringComparison.Ordinal);
            (cut.FindAll(ButtonSelector)).ShouldContain(button => button.TextContent.Contains("Record Payment", StringComparison.Ordinal));
        });
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Record_payment_non_success_outcome_shows_sanitized_error()
    {
        // Arrange
        var booking = BuildBookingDto(status: BookingStatusDto.Pending, remainingBalance: 250m);
        _fakeBookingsApi.AddBooking(booking);
        _fakeBookingsApi.SetRecordPaymentOutcome(ContractCommandOutcome.Status(ContractCommandOutcomeKind.Conflict, HttpStatusCode.Conflict));

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find(HeadingOrAlertSelector));

        // Act
        var showButton = cut.FindAll(ButtonSelector)
            .First(button => button.TextContent.Contains("Record Payment", StringComparison.Ordinal));
        await cut.InvokeAsync(() => showButton.Click());
        await cut.WaitForAssertionAsync(() => cut.FindComponent<PaymentForm>());

        var form = cut.FindComponent<PaymentForm>();
        form.Instance.Model.Amount = 100m;
        form.Instance.Model.Method = PaymentMethodDto.Cash;
        await cut.InvokeAsync(() => form.Find("form").Submit());

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            cut.Markup.ShouldContain("We couldn't record the payment right now. Please try again.", StringComparison.Ordinal);
            cut.Markup.ShouldContain("Record New Payment", StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Page_has_correct_title()
    {
        // Arrange
        var booking = BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find(HeadingOrAlertSelector));
        // Assert
        var pageTitle = cut.Find("h1");
        (pageTitle.TextContent.Trim()).ShouldBe("Edit Booking");
    }

    [Fact]
    public async Task Status_badge_updates_after_confirm_action()
    {
        // Arrange
        var booking = BuildBookingDto(status: BookingStatusDto.Pending);
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find(HeadingOrAlertSelector));

        // Act — click the Confirm Booking button
        var confirmButton = cut.FindAll(ButtonSelector).First(b => b.TextContent.Contains("Confirm Booking", StringComparison.Ordinal));
        await cut.InvokeAsync((Action)(() => confirmButton.Click()));

        // Assert — status badge should now show Confirmed
        var statusBadge = cut.FindComponent<BookingStatusBadge>();
        statusBadge.Instance.Status.ShouldBe(BookingStatusDto.Confirmed);
    }

    [Fact]
    public async Task Status_badge_updates_after_complete_action()
    {
        // Arrange
        var booking = BuildBookingDto(status: BookingStatusDto.Confirmed);
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find(HeadingOrAlertSelector));

        // Act — click the Complete Booking button
        var completeButton = cut.FindAll(ButtonSelector).First(b => b.TextContent.Contains("Complete Booking", StringComparison.Ordinal));
        await cut.InvokeAsync((Action)(() => completeButton.Click()));

        // Assert — status badge should now show Completed
        var statusBadge = cut.FindComponent<BookingStatusBadge>();
        statusBadge.Instance.Status.ShouldBe(BookingStatusDto.Completed);
    }

    [Theory]
    [InlineData("confirm", BookingStatusDto.Pending, "Confirm Booking", null)]
    [InlineData("complete", BookingStatusDto.Confirmed, "Complete Booking", null)]
    [InlineData("cancel", BookingStatusDto.Confirmed, "Cancel Booking", "Yes, Cancel")]
    [InlineData("delete", BookingStatusDto.Pending, "Delete Booking", "Yes, Delete")]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Action_controls_are_disabled_while_booking_request_is_in_flight(
        string actionKind,
        BookingStatusDto status,
        string actionText,
        string? confirmationText)
    {
        // Arrange
        var booking = BuildBookingDto(status: status);
        _fakeBookingsApi.AddBooking(booking);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var actionGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var actionGateCancellation = cancellationToken.Register(() => actionGate.TrySetCanceled(cancellationToken));
        _fakeBookingsApi.SetBookingActionTask(actionGate.Task);
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find(".card.border-danger"));
        var actionButton = cut.FindAll(".card.border-danger button")
            .ShouldHaveSingleItem(button => button.TextContent.Contains(actionText, StringComparison.Ordinal));

        var actionTask = Task.Run(
            async () => await actionButton.ClickAsync(new MouseEventArgs()),
            cancellationToken);
        if (actionKind is "cancel" or "delete")
        {
            await cut.WaitForAssertionAsync(() => cut.Find(VisibleModalSelector));
            var expectedConfirmationText = confirmationText.ShouldNotBeNull();
            var confirmationButton = cut.FindAll($"{VisibleModalSelector} button")
                .ShouldHaveSingleItem(button => button.TextContent.Contains(expectedConfirmationText, StringComparison.Ordinal));
            await confirmationButton.ClickAsync(new MouseEventArgs());
        }

        // Act
        await cut.WaitForStateAsync(() => _fakeBookingsApi.BookingActionCallCount == 1);
        await cut.WaitForAssertionAsync(() =>
        {
            var actionButtons = cut.FindAll(".card.border-danger button");
            actionButtons.ShouldNotBeEmpty();
            actionButtons.ShouldAllSatisfy(button => button.HasAttribute(DisabledAttributeName).ShouldBeTrue());
        });
        actionGate.SetResult();
        await actionTask;

        // Assert
        _fakeBookingsApi.BookingActionCallCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(BookingStatusDto.Pending)]
    [InlineData(BookingStatusDto.Confirmed)]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Disabled_action_controls_do_not_dispatch_callbacks(BookingStatusDto status)
    {
        // Arrange
        var callbackCount = 0;
        var cut = Render<BookingEditActionPanel>(parameters => parameters
            .Add(panel => panel.Status, status)
            .Add(panel => panel.IsSubmitting, true)
            .Add(panel => panel.Confirm, EventCallback.Factory.Create(this, () => callbackCount++))
            .Add(panel => panel.Complete, EventCallback.Factory.Create(this, () => callbackCount++))
            .Add(panel => panel.Cancel, EventCallback.Factory.Create(this, () => callbackCount++))
            .Add(panel => panel.Delete, EventCallback.Factory.Create(this, () => callbackCount++)));

        // Act
        var buttons = cut.FindAll("button");
        foreach (var button in buttons)
        {
            await button.ClickAsync(new MouseEventArgs());
        }

        // Assert
        buttons.ShouldNotBeEmpty();
        buttons.ShouldAllSatisfy(button => button.HasAttribute(DisabledAttributeName).ShouldBeTrue());
        callbackCount.ShouldBe(0);
    }

    [Fact]
    public async Task Cancel_booking_should_show_configured_confirm_dialog()
    {
        // Arrange
        var booking = BuildBookingDto(status: BookingStatusDto.Confirmed);
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find(HeadingOrAlertSelector));

        // Act
        var cancelButton = cut.FindAll(ButtonSelector).First(button => button.TextContent.Contains(CancelBookingText, StringComparison.Ordinal));
        await cut.InvokeAsync((Action)(() => cancelButton.Click()));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var dialog = cut.Find(VisibleModalSelector);
            (dialog.TextContent).ShouldContain(CancelBookingText, StringComparison.Ordinal);
            (dialog.TextContent).ShouldContain("Are you sure you want to cancel this booking?", StringComparison.Ordinal);
            (dialog.TextContent).ShouldContain("Yes, Cancel", StringComparison.Ordinal);
            (dialog.TextContent).ShouldContain("No", StringComparison.Ordinal);

            var confirmButton = dialog.QuerySelector(".modal-footer .btn-warning");
            _ = (confirmButton).ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task Cancelling_cancel_dialog_should_keep_status_unchanged()
    {
        // Arrange
        var booking = BuildBookingDto(status: BookingStatusDto.Confirmed);
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find(HeadingOrAlertSelector));

        // Act
        var cancelButton = cut.FindAll(ButtonSelector).First(button => button.TextContent.Contains(CancelBookingText, StringComparison.Ordinal));
        await cut.InvokeAsync((Action)(() => cancelButton.Click()));
        await cut.WaitForAssertionAsync(() => cut.Find(VisibleModalSelector));

        var noButton = cut.FindAll(ButtonSelector).First(button => button.TextContent.Trim() == "No");
        await cut.InvokeAsync((Action)(() => noButton.Click()));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            (cut.FindAll(VisibleModalSelector)).ShouldBeEmpty();

            var statusBadge = cut.FindComponent<BookingStatusBadge>();
            statusBadge.Instance.Status.ShouldBe(BookingStatusDto.Confirmed);

            var cancelButtons = cut.FindAll(ButtonSelector).Where(button => button.TextContent.Contains(CancelBookingText, StringComparison.Ordinal));
            (cancelButtons).ShouldNotBeEmpty();
        });
    }

    [Fact]
    public async Task Confirming_cancel_booking_should_update_status_and_disable_editing()
    {
        // Arrange
        var booking = BuildBookingDto(status: BookingStatusDto.Confirmed, notes: "Original notes");
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find(HeadingOrAlertSelector));

        // Act
        var cancelButton = cut.FindAll(ButtonSelector).First(button => button.TextContent.Contains(CancelBookingText, StringComparison.Ordinal));
        await cut.InvokeAsync((Action)(() => cancelButton.Click()));
        await cut.WaitForAssertionAsync(() => cut.Find(VisibleModalSelector));

        var confirmButton = cut.FindAll(ButtonSelector).First(button => button.TextContent.Contains("Yes, Cancel", StringComparison.Ordinal));
        await cut.InvokeAsync((Action)(() => confirmButton.Click()));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var warning = cut.Find(".alert.alert-warning");
            (warning.TextContent).ShouldContain("cancelled", StringComparison.OrdinalIgnoreCase);

            var statusBadge = cut.FindComponent<BookingStatusBadge>();
            statusBadge.Instance.Status.ShouldBe(BookingStatusDto.Cancelled);

            var notes = cut.Find("#notes");
            (notes.HasAttribute(DisabledAttributeName)).ShouldBeTrue();

            var roomType = cut.Find("#roomType");
            roomType.HasAttribute(DisabledAttributeName).ShouldBeTrue();

            var principalBikeType = cut.Find("#principalBikeType");
            principalBikeType.HasAttribute(DisabledAttributeName).ShouldBeTrue();

            (cut.FindAll(ButtonSelector)).ShouldNotContain(button => button.TextContent.Contains(CancelBookingText, StringComparison.Ordinal));
            (cut.FindAll(ButtonSelector)).ShouldContain(button => button.TextContent.Contains(DeleteBookingText, StringComparison.Ordinal));
        });
    }

    [Fact]
    public async Task Delete_booking_should_show_configured_confirm_dialog()
    {
        // Arrange
        var booking = BuildBookingDto(status: BookingStatusDto.Pending);
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find(HeadingOrAlertSelector));

        // Act
        var deleteButton = cut.FindAll(ButtonSelector).First(button => button.TextContent.Contains(DeleteBookingText, StringComparison.Ordinal));
        await cut.InvokeAsync((Action)(() => deleteButton.Click()));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var dialog = cut.Find(VisibleModalSelector);
            (dialog.TextContent).ShouldContain(DeleteBookingText, StringComparison.Ordinal);
            (dialog.TextContent).ShouldContain("cannot be undone", StringComparison.Ordinal);
            (dialog.TextContent).ShouldContain("Yes, Delete", StringComparison.Ordinal);
            (dialog.TextContent).ShouldContain("No", StringComparison.Ordinal);

            var confirmButton = dialog.QuerySelector(".modal-footer .btn-danger");
            _ = (confirmButton).ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task Cancelling_delete_dialog_should_keep_booking_on_edit_page()
    {
        // Arrange
        var booking = BuildBookingDto(status: BookingStatusDto.Pending);
        _fakeBookingsApi.AddBooking(booking);
        var navigationManager = Services.GetRequiredService<NavigationManager>();

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find(HeadingOrAlertSelector));

        // Act
        var deleteButton = cut.FindAll(ButtonSelector).First(button => button.TextContent.Contains(DeleteBookingText, StringComparison.Ordinal));
        await cut.InvokeAsync((Action)(() => deleteButton.Click()));
        await cut.WaitForAssertionAsync(() => cut.Find(VisibleModalSelector));

        var noButton = cut.FindAll(ButtonSelector).First(button => button.TextContent.Trim() == "No");
        await cut.InvokeAsync((Action)(() => noButton.Click()));

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            (cut.FindAll(VisibleModalSelector)).ShouldBeEmpty();
            (cut.FindAll(ButtonSelector)).ShouldContain(button => button.TextContent.Contains(DeleteBookingText, StringComparison.Ordinal));
            (navigationManager.Uri).ShouldNotContain("/bookings", StringComparison.OrdinalIgnoreCase);
        });
    }
}
