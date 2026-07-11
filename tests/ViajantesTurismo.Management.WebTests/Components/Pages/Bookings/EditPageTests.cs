using System.Net;
using Microsoft.AspNetCore.Components;
using SharedKernel.HttpClients;
using ViajantesTurismo.Management.Web.Components.Pages.Bookings;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Bookings;

public sealed class EditPageTests : BunitContext
{
    private const string HeadingOrAlertSelector = "h1, .alert";
    private const string ValueAttributeName = "value";
    private const string StatusSelector = "#status";
    private const string DisabledAttributeName = "disabled";
    private const string ButtonSelector = "button";
    private const string CancelBookingText = "Cancel Booking";
    private const string DeleteBookingText = "Delete Booking";
    private const string VisibleModalSelector = ".modal.show";

    private readonly FakeBookingsApiClient _fakeBookingsApi = new();

    public EditPageTests()
    {
        Services.AddSingleton<IBookingsApiClient>(_fakeBookingsApi);
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
        TestAssert.Equal("Edit Booking", heading.TextContent.Trim());
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
        TestAssert.Contains("We couldn't load the booking right now. Please try again.", alert.TextContent, StringComparison.Ordinal);
        TestAssert.DoesNotContain("SQL timeout", alert.TextContent, StringComparison.Ordinal);
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
            TestAssert.Contains("We couldn't update the booking right now. Please try again.", alert.TextContent, StringComparison.Ordinal);
            TestAssert.DoesNotContain("Update failed hard", alert.TextContent, StringComparison.Ordinal);
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
        TestAssert.Contains("Portugal Adventure", tourLink.TextContent, StringComparison.Ordinal);

        var tourIdentifier = cut.Find("small.text-muted");
        TestAssert.Contains("PT-2024-001", tourIdentifier.TextContent, StringComparison.Ordinal);

        var customerLink = cut.Find($"a[href='/customers/{booking.CustomerId}']");
        TestAssert.Contains("John Doe", customerLink.TextContent, StringComparison.Ordinal);
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
        TestAssert.Contains("Jane Smith", companionLink.TextContent, StringComparison.Ordinal);
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
        TestAssert.DoesNotContain("Companion:", html, StringComparison.Ordinal);
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
        TestAssert.Contains("15/03/2024 14:30", html, StringComparison.Ordinal);
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
        TestAssert.Equal("R$ 3,250.50", priceInput.GetAttribute(ValueAttributeName));
        TestAssert.True(priceInput.HasAttribute("readonly"));
    }

    [Fact]
    public void Renders_status_dropdown_with_all_options()
    {
        // Arrange
        var booking = BuildBookingDto(status: BookingStatusDto.Pending);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find(HeadingOrAlertSelector));
        // Assert
        var statusSelect = cut.Find(StatusSelector);
        var options = statusSelect.QuerySelectorAll("option");
        TestAssert.Equal(4, options.Length);

        TestAssert.Contains(options, o => o.TextContent.Contains("Pending", StringComparison.Ordinal));
        TestAssert.Contains(options, o => o.TextContent.Contains("Confirmed", StringComparison.Ordinal));
        TestAssert.Contains(options, o => o.TextContent.Contains("Completed", StringComparison.Ordinal));
        TestAssert.Contains(options, o => o.TextContent.Contains("Cancelled", StringComparison.Ordinal));
    }

    [Fact]
    public void Renders_payment_status_dropdown_with_all_options()
    {
        // Arrange
        var booking = BuildBookingDto(paymentStatus: PaymentStatusDto.Unpaid);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find(HeadingOrAlertSelector));
        // Assert
        var paymentStatusSelect = cut.Find("#paymentStatus");
        var options = paymentStatusSelect.QuerySelectorAll("option");
        TestAssert.Equal(4, options.Length);

        TestAssert.Contains(options, o => o.TextContent.Contains("Unpaid", StringComparison.Ordinal));
        TestAssert.Contains(options, o => o.TextContent.Contains("Partially Paid", StringComparison.Ordinal));
        TestAssert.Contains(options, o => o.TextContent.Contains("Paid", StringComparison.Ordinal));
        TestAssert.Contains(options, o => o.TextContent.Contains("Refunded", StringComparison.Ordinal));
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
        TestAssert.Contains("Customer requested early check-in", value, StringComparison.Ordinal);
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
        TestAssert.Equal(3, options.Length);

        TestAssert.Contains(options, o => o.TextContent.Contains("No Discount", StringComparison.Ordinal));
        TestAssert.Contains(options, o => o.TextContent.Contains("Percentage", StringComparison.Ordinal));
        TestAssert.Contains(options, o => o.TextContent.Contains("Absolute Amount", StringComparison.Ordinal));
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
        TestAssert.DoesNotContain("discountAmount", html, StringComparison.Ordinal);
        TestAssert.DoesNotContain("discountReason", html, StringComparison.Ordinal);
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
        TestAssert.Contains("Discount Percentage", label.TextContent, StringComparison.Ordinal);

        var formText = cut.Find("#discountAmount + .form-text");
        TestAssert.Contains("between 0 and 100", formText.TextContent, StringComparison.Ordinal);

        var discountReason = cut.Find("#discountReason");
        var reasonValue = discountReason.GetAttribute(ValueAttributeName) ?? discountReason.TextContent;
        TestAssert.Contains("Early bird discount", reasonValue, StringComparison.Ordinal);
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
        TestAssert.Contains("Discount Amount", label.TextContent, StringComparison.Ordinal);
        TestAssert.DoesNotContain("Percentage", label.TextContent, StringComparison.Ordinal);
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
        TestAssert.Equal("/bookings", cancelLink.GetAttribute("href"));
        TestAssert.Contains("Cancel", cancelLink.TextContent, StringComparison.Ordinal);
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
        TestAssert.Contains("Update Booking", submitButton.TextContent, StringComparison.Ordinal);
        TestAssert.False(submitButton.HasAttribute(DisabledAttributeName));
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
        _ = TestAssert.NotNull(cancelButton);
        await cut.InvokeAsync(() => cancelButton.Click());

        // Assert
        var successAlert = cut.Find(".alert.alert-success");
        TestAssert.Contains("Booking updated successfully!", successAlert.TextContent, StringComparison.Ordinal);

        var goToBookingsButton = cut.Find($".alert.alert-success {ButtonSelector}");
        TestAssert.Contains("Go to Bookings", goToBookingsButton.TextContent, StringComparison.Ordinal);
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
        TestAssert.True(submitButton.HasAttribute(DisabledAttributeName));
        TestAssert.Contains("Updating...", submitButton.TextContent, StringComparison.Ordinal);

        var spinner = submitButton.QuerySelector(".spinner-border");
        _ = TestAssert.NotNull(spinner);

        var cancelLink = cut.Find("a.btn.btn-secondary");
        TestAssert.Contains(DisabledAttributeName, cancelLink.GetAttribute("class"), StringComparison.Ordinal);

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
        TestAssert.Contains("Required for audit purposes", helpText.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("10-500 characters", helpText.TextContent, StringComparison.Ordinal);
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
            TestAssert.Contains("Record New Payment", cut.Markup, StringComparison.Ordinal);
            TestAssert.NotEmpty(cut.FindAll("#amount"));
        });

        // Act
        var hideButton = cut.FindAll(ButtonSelector)
            .First(button => button.TextContent.Trim() == "Cancel");
        await cut.InvokeAsync(() => hideButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            TestAssert.DoesNotContain("Record New Payment", cut.Markup, StringComparison.Ordinal);
            TestAssert.Contains(cut.FindAll(ButtonSelector), button => button.TextContent.Contains("Record Payment", StringComparison.Ordinal));
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
        TestAssert.Equal("Edit Booking", pageTitle.TextContent.Trim());
    }

    [Fact]
    public async Task Status_dropdown_updates_after_confirm_action()
    {
        // Arrange
        var booking = BuildBookingDto(status: BookingStatusDto.Pending);
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find(HeadingOrAlertSelector));

        // Act — click the Confirm Booking button
        var confirmButton = cut.FindAll(ButtonSelector).First(b => b.TextContent.Contains("Confirm Booking", StringComparison.Ordinal));
        await cut.InvokeAsync((Action)(() => confirmButton.Click()));

        // Assert — status dropdown should now show Confirmed
        var statusSelect = cut.Find(StatusSelector);
        TestAssert.Equal(nameof(BookingStatusDto.Confirmed), statusSelect.GetAttribute(ValueAttributeName));
    }

    [Fact]
    public async Task Status_dropdown_updates_after_complete_action()
    {
        // Arrange
        var booking = BuildBookingDto(status: BookingStatusDto.Confirmed);
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find(HeadingOrAlertSelector));

        // Act — click the Complete Booking button
        var completeButton = cut.FindAll(ButtonSelector).First(b => b.TextContent.Contains("Complete Booking", StringComparison.Ordinal));
        await cut.InvokeAsync((Action)(() => completeButton.Click()));

        // Assert — status dropdown should now show Completed
        var statusSelect = cut.Find(StatusSelector);
        TestAssert.Equal(nameof(BookingStatusDto.Completed), statusSelect.GetAttribute(ValueAttributeName));
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
            TestAssert.Contains(CancelBookingText, dialog.TextContent, StringComparison.Ordinal);
            TestAssert.Contains("Are you sure you want to cancel this booking?", dialog.TextContent, StringComparison.Ordinal);
            TestAssert.Contains("Yes, Cancel", dialog.TextContent, StringComparison.Ordinal);
            TestAssert.Contains("No", dialog.TextContent, StringComparison.Ordinal);

            var confirmButton = dialog.QuerySelector(".modal-footer .btn-warning");
            _ = TestAssert.NotNull(confirmButton);
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
            TestAssert.Empty(cut.FindAll(VisibleModalSelector));

            var statusSelect = cut.Find(StatusSelector);
            TestAssert.Equal(nameof(BookingStatusDto.Confirmed), statusSelect.GetAttribute(ValueAttributeName));

            var cancelButtons = cut.FindAll(ButtonSelector).Where(button => button.TextContent.Contains(CancelBookingText, StringComparison.Ordinal));
            TestAssert.NotEmpty(cancelButtons);
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
            TestAssert.Contains("cancelled", warning.TextContent, StringComparison.OrdinalIgnoreCase);

            var statusSelect = cut.Find(StatusSelector);
            TestAssert.Equal(nameof(BookingStatusDto.Cancelled), statusSelect.GetAttribute(ValueAttributeName));
            TestAssert.True(statusSelect.HasAttribute(DisabledAttributeName));

            var notes = cut.Find("#notes");
            TestAssert.True(notes.HasAttribute(DisabledAttributeName));

            TestAssert.DoesNotContain(cut.FindAll(ButtonSelector), button => button.TextContent.Contains(CancelBookingText, StringComparison.Ordinal));
            TestAssert.Contains(cut.FindAll(ButtonSelector), button => button.TextContent.Contains(DeleteBookingText, StringComparison.Ordinal));
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
            TestAssert.Contains(DeleteBookingText, dialog.TextContent, StringComparison.Ordinal);
            TestAssert.Contains("cannot be undone", dialog.TextContent, StringComparison.Ordinal);
            TestAssert.Contains("Yes, Delete", dialog.TextContent, StringComparison.Ordinal);
            TestAssert.Contains("No", dialog.TextContent, StringComparison.Ordinal);

            var confirmButton = dialog.QuerySelector(".modal-footer .btn-danger");
            _ = TestAssert.NotNull(confirmButton);
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
            TestAssert.Empty(cut.FindAll(VisibleModalSelector));
            TestAssert.Contains(cut.FindAll(ButtonSelector), button => button.TextContent.Contains(DeleteBookingText, StringComparison.Ordinal));
            TestAssert.DoesNotContain("/bookings", navigationManager.Uri, StringComparison.OrdinalIgnoreCase);
        });
    }
}
