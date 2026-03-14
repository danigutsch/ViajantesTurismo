using Microsoft.AspNetCore.Components.Forms;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Tests.Shared.Builders;
using ViajantesTurismo.Admin.Tests.Shared.Fakes.ApiClients;
using ViajantesTurismo.Admin.Web.Components.Pages.Bookings;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Bookings;

public sealed class EditPageTests : BunitContext
{
    private readonly FakeBookingsApiClient _fakeBookingsApi = new();

    public EditPageTests()
    {
        Services.AddSingleton<IBookingsApiClient>(_fakeBookingsApi);
    }

    [Fact]
    public void Page_Renders_Successfully_With_Valid_Id()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var heading = cut.Find("h1");
        Assert.Equal("Edit Booking", heading.TextContent.Trim());
    }

    [Fact]
    public void Displays_Not_Found_When_Booking_Does_Not_Exist()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, bookingId));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var alert = cut.Find(".alert.alert-danger");
        Assert.Contains("Booking not found", alert.TextContent, StringComparison.Ordinal);

        var backLink = cut.Find("a.btn.btn-secondary");
        Assert.Equal("/bookings", backLink.GetAttribute("href"));
    }

    [Fact]
    public void OnInitializedAsync_When_Load_Fails_Shows_Sanitized_Error_Message()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        _fakeBookingsApi.SetGetBookingByIdException(new InvalidOperationException("SQL timeout"));

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, bookingId));
        cut.WaitForAssertion(() => cut.Find(".alert.alert-danger"));

        // Assert
        var alert = cut.Find(".alert.alert-danger");
        Assert.Contains("We couldn't load the booking right now. Please try again.", alert.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("SQL timeout", alert.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleSubmit_When_Update_Fails_Shows_Sanitized_Error_Message()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(notes: "Original notes");
        _fakeBookingsApi.AddBooking(booking);
        _fakeBookingsApi.SetUpdateBookingNotesException(new InvalidOperationException("Update failed hard"));

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("form"));

        // Act
        var notesTextarea = cut.Find("#notes");
        await cut.InvokeAsync(() => notesTextarea.Change("Updated notes"));

        var form = cut.Find("form");
        await cut.InvokeAsync(async () => await form.SubmitAsync());

        // Assert
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".alert.alert-danger");
            Assert.Contains("We couldn't update the booking right now. Please try again.", alert.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Update failed hard", alert.TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Displays_Booking_Information_When_Found()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(
            tourName: "Portugal Adventure",
            tourIdentifier: "PT-2024-001",
            customerName: "John Doe",
            totalPrice: 2500m
        );
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var tourLink = cut.Find($"a[href='/tours/{booking.TourId}']");
        Assert.Contains("Portugal Adventure", tourLink.TextContent, StringComparison.Ordinal);

        var tourIdentifier = cut.Find("small.text-muted");
        Assert.Contains("PT-2024-001", tourIdentifier.TextContent, StringComparison.Ordinal);

        var customerLink = cut.Find($"a[href='/customers/{booking.CustomerId}']");
        Assert.Contains("John Doe", customerLink.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_Companion_Information_When_Present()
    {
        // Arrange
        var companionId = Guid.NewGuid();
        var booking = DtoBuilders.BuildBookingDto(
            companionId: companionId,
            companionName: "Jane Smith"
        );
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var companionLink = cut.Find($"a[href='/customers/{companionId}']");
        Assert.Contains("Jane Smith", companionLink.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Does_Not_Display_Companion_When_Not_Present()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(companionId: null);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var html = cut.Markup;
        Assert.DoesNotContain("Companion:", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_Booking_Date_In_Correct_Format()
    {
        // Arrange
        var bookingDate = new DateTime(2024, 3, 15, 14, 30, 0);
        var booking = DtoBuilders.BuildBookingDto(bookingDate: bookingDate);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var html = cut.Markup;
        Assert.Contains("15/03/2024 14:30", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_Total_Price_As_Readonly()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(totalPrice: 3250.50m);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var priceInput = cut.Find("#totalPrice");
        Assert.Equal("R$ 3,250.50", priceInput.GetAttribute("value"));
        Assert.True(priceInput.HasAttribute("readonly"));
    }

    [Fact]
    public void Renders_Status_Dropdown_With_All_Options()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(status: BookingStatusDto.Pending);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var statusSelect = cut.Find("#status");
        var options = statusSelect.QuerySelectorAll("option");
        Assert.Equal(4, options.Length);

        Assert.Contains(options, o => o.TextContent.Contains("Pending", StringComparison.Ordinal));
        Assert.Contains(options, o => o.TextContent.Contains("Confirmed", StringComparison.Ordinal));
        Assert.Contains(options, o => o.TextContent.Contains("Completed", StringComparison.Ordinal));
        Assert.Contains(options, o => o.TextContent.Contains("Cancelled", StringComparison.Ordinal));
    }

    [Fact]
    public void Renders_Payment_Status_Dropdown_With_All_Options()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(paymentStatus: PaymentStatusDto.Unpaid);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var paymentStatusSelect = cut.Find("#paymentStatus");
        var options = paymentStatusSelect.QuerySelectorAll("option");
        Assert.Equal(4, options.Length);

        Assert.Contains(options, o => o.TextContent.Contains("Unpaid", StringComparison.Ordinal));
        Assert.Contains(options, o => o.TextContent.Contains("Partially Paid", StringComparison.Ordinal));
        Assert.Contains(options, o => o.TextContent.Contains("Paid", StringComparison.Ordinal));
        Assert.Contains(options, o => o.TextContent.Contains("Refunded", StringComparison.Ordinal));
    }

    [Fact]
    public void Preloads_Existing_Notes_In_Form()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(notes: "Customer requested early check-in");
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var notesTextarea = cut.Find("#notes");
        var value = notesTextarea.GetAttribute("value") ?? notesTextarea.TextContent;
        Assert.Contains("Customer requested early check-in", value, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_Discount_Type_Dropdown_With_All_Options()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var discountTypeSelect = cut.Find("#discountType");
        var options = discountTypeSelect.QuerySelectorAll("option");
        Assert.Equal(3, options.Length);

        Assert.Contains(options, o => o.TextContent.Contains("No Discount", StringComparison.Ordinal));
        Assert.Contains(options, o => o.TextContent.Contains("Percentage", StringComparison.Ordinal));
        Assert.Contains(options, o => o.TextContent.Contains("Absolute Amount", StringComparison.Ordinal));
    }

    [Fact]
    public void Does_Not_Show_Discount_Fields_When_Type_Is_None()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(discountType: DiscountTypeDto.None);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var html = cut.Markup;
        Assert.DoesNotContain("discountAmount", html, StringComparison.Ordinal);
        Assert.DoesNotContain("discountReason", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_Discount_Amount_Field_When_Percentage_Selected()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(
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
        Assert.Contains("Discount Percentage", label.TextContent, StringComparison.Ordinal);

        var formText = cut.Find("#discountAmount + .form-text");
        Assert.Contains("between 0 and 100", formText.TextContent, StringComparison.Ordinal);

        var discountReason = cut.Find("#discountReason");
        var reasonValue = discountReason.GetAttribute("value") ?? discountReason.TextContent;
        Assert.Contains("Early bird discount", reasonValue, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_Discount_Amount_Field_When_Absolute_Selected()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(
            discountType: DiscountTypeDto.Absolute,
            discountAmount: 250m,
            discountReason: "Group discount"
        );
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var label = cut.Find("label[for='discountAmount']");
        Assert.Contains("Discount Amount", label.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Percentage", label.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_Cancel_Button_With_Link_To_Bookings()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var cancelLink = cut.Find("a.btn.btn-secondary");
        Assert.Equal("/bookings", cancelLink.GetAttribute("href"));
        Assert.Contains("Cancel", cancelLink.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Submit_Button_Shows_Default_Text_Initially()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var submitButton = cut.Find("button[type='submit']");
        Assert.Contains("Update Booking", submitButton.TextContent, StringComparison.Ordinal);
        Assert.False(submitButton.HasAttribute("disabled"));
    }

    [Fact]
    public async Task Shows_Success_Message_After_Successful_Update()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(notes: "Original notes");
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));

        // Act - Update notes
        var notesTextarea = cut.Find("#notes");
        await cut.InvokeAsync(() => notesTextarea.Change("Updated notes"));

        var form = cut.Find("form");
        await cut.InvokeAsync(async () => await form.SubmitAsync());

        // Assert
        var successAlert = cut.Find(".alert.alert-success");
        Assert.Contains("Booking updated successfully!", successAlert.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Shows_Pending_Redirect_Message_After_Success()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));

        // Act - Submit form
        var form = cut.Find("form");
        await cut.InvokeAsync(async () => await form.SubmitAsync());

        // Assert
        var redirectAlert = cut.FindAll(".alert.alert-info").First(a => a.TextContent.Contains("Redirecting", StringComparison.Ordinal));
        Assert.Contains("Redirecting to bookings page in 3 seconds", redirectAlert.TextContent, StringComparison.Ordinal);

        var cancelButton = redirectAlert.QuerySelector("button");
        Assert.NotNull(cancelButton);
        Assert.Contains("Cancel", cancelButton.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cancel_Redirect_Button_Shows_Success_Message()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));

        // Act - Submit and then cancel redirect
        var form = cut.Find("form");
        await cut.InvokeAsync(async () => await form.SubmitAsync());

        var redirectAlert = cut.FindAll(".alert.alert-info").First(a => a.TextContent.Contains("Redirecting", StringComparison.Ordinal));
        var cancelButton = redirectAlert.QuerySelector("button");
        Assert.NotNull(cancelButton);
        await cut.InvokeAsync(() => cancelButton.Click());

        // Assert
        var successAlert = cut.Find(".alert.alert-success");
        Assert.Contains("Booking updated successfully!", successAlert.TextContent, StringComparison.Ordinal);

        var goToBookingsButton = cut.Find(".alert.alert-success button");
        Assert.Contains("Go to Bookings", goToBookingsButton.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Buttons_Are_Disabled_During_Submission()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));

        // Act - Start submission
        var form = cut.Find("form");
        var submitTask = cut.InvokeAsync(async () => await form.SubmitAsync());

        // Assert - During submission
        var submitButton = cut.Find("button[type='submit']");
        Assert.True(submitButton.HasAttribute("disabled"));
        Assert.Contains("Updating...", submitButton.TextContent, StringComparison.Ordinal);

        var spinner = submitButton.QuerySelector(".spinner-border");
        Assert.NotNull(spinner);

        var cancelLink = cut.Find("a.btn.btn-secondary");
        Assert.Contains("disabled", cancelLink.GetAttribute("class"), StringComparison.Ordinal);

        await submitTask;
    }

    [Fact]
    public void Form_Has_DataAnnotationsValidator()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var validator = cut.FindComponent<DataAnnotationsValidator>();
        Assert.NotNull(validator);
    }

    [Fact]
    public void Form_Has_ValidationSummary()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var validationSummary = cut.FindComponent<ValidationSummary>();
        Assert.NotNull(validationSummary);
    }

    [Fact]
    public void Displays_Discount_Reason_Help_Text()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(
            discountType: DiscountTypeDto.Percentage,
            discountAmount: 10m,
            discountReason: "Test"
        );
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var helpText = cut.Find("#discountReason + .form-text");
        Assert.Contains("Required for audit purposes", helpText.TextContent, StringComparison.Ordinal);
        Assert.Contains("10-500 characters", helpText.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Page_Has_Correct_Title()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1, .alert"));
        // Assert
        var pageTitle = cut.Find("h1");
        Assert.Equal("Edit Booking", pageTitle.TextContent.Trim());
    }

    [Fact]
    public async Task Status_Dropdown_Updates_After_Confirm_Action()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(status: BookingStatusDto.Pending);
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("h1, .alert"));

        // Act — click Confirm Booking button
        var confirmButton = cut.FindAll("button").First(b => b.TextContent.Contains("Confirm Booking", StringComparison.Ordinal));
        await cut.InvokeAsync((Action)(() => confirmButton.Click()));

        // Assert — status dropdown should now show Confirmed
        var statusSelect = cut.Find("#status");
        Assert.Equal(nameof(BookingStatusDto.Confirmed), statusSelect.GetAttribute("value"));
    }

    [Fact]
    public async Task Status_Dropdown_Updates_After_Complete_Action()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(status: BookingStatusDto.Confirmed);
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("h1, .alert"));

        // Act — click Complete Booking button
        var completeButton = cut.FindAll("button").First(b => b.TextContent.Contains("Complete Booking", StringComparison.Ordinal));
        await cut.InvokeAsync((Action)(() => completeButton.Click()));

        // Assert — status dropdown should now show Completed
        var statusSelect = cut.Find("#status");
        Assert.Equal(nameof(BookingStatusDto.Completed), statusSelect.GetAttribute("value"));
    }
}
