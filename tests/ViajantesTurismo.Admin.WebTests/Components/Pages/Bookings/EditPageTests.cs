using Microsoft.AspNetCore.Components.Forms;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Tests.Shared;
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
    public async Task Page_Renders_Successfully_With_Valid_Id()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var heading = cut.Find("h1");
        Assert.Equal("Edit Booking", heading.TextContent.Trim());
    }

    [Fact]
    public async Task Displays_Not_Found_When_Booking_Does_Not_Exist()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, bookingId));
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var alert = cut.Find(".alert.alert-danger");
        Assert.Contains("Booking not found", alert.TextContent);

        var backLink = cut.Find("a.btn.btn-secondary");
        Assert.Equal("/bookings", backLink.GetAttribute("href"));
    }

    [Fact]
    public async Task Displays_Booking_Information_When_Found()
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
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var tourLink = cut.Find($"a[href='/tours/{booking.TourId}']");
        Assert.Contains("Portugal Adventure", tourLink.TextContent);

        var tourIdentifier = cut.Find("small.text-muted");
        Assert.Contains("PT-2024-001", tourIdentifier.TextContent);

        var customerLink = cut.Find($"a[href='/customers/{booking.CustomerId}']");
        Assert.Contains("John Doe", customerLink.TextContent);
    }

    [Fact]
    public async Task Displays_Companion_Information_When_Present()
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
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var companionLink = cut.Find($"a[href='/customers/{companionId}']");
        Assert.Contains("Jane Smith", companionLink.TextContent);
    }

    [Fact]
    public async Task Does_Not_Display_Companion_When_Not_Present()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(companionId: null);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var html = cut.Markup;
        Assert.DoesNotContain("Companion:", html);
    }

    [Fact]
    public async Task Displays_Booking_Date_In_Correct_Format()
    {
        // Arrange
        var bookingDate = new DateTime(2024, 3, 15, 14, 30, 0);
        var booking = DtoBuilders.BuildBookingDto(bookingDate: bookingDate);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var html = cut.Markup;
        Assert.Contains("15/03/2024 14:30", html);
    }

    [Fact]
    public async Task Displays_Total_Price_As_Readonly()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(totalPrice: 3250.50m);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var priceInput = cut.Find("#totalPrice");
        Assert.Equal("3250.50", priceInput.GetAttribute("value"));
        Assert.True(priceInput.HasAttribute("readonly"));
    }

    [Fact]
    public async Task Renders_Status_Dropdown_With_All_Options()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(status: BookingStatusDto.Pending);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var statusSelect = cut.Find("#status");
        var options = statusSelect.QuerySelectorAll("option");
        Assert.Equal(4, options.Length);

        Assert.Contains(options, o => o.TextContent.Contains("Pending"));
        Assert.Contains(options, o => o.TextContent.Contains("Confirmed"));
        Assert.Contains(options, o => o.TextContent.Contains("Completed"));
        Assert.Contains(options, o => o.TextContent.Contains("Cancelled"));
    }

    [Fact]
    public async Task Renders_Payment_Status_Dropdown_With_All_Options()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(paymentStatus: PaymentStatusDto.Unpaid);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var paymentStatusSelect = cut.Find("#paymentStatus");
        var options = paymentStatusSelect.QuerySelectorAll("option");
        Assert.Equal(4, options.Length);

        Assert.Contains(options, o => o.TextContent.Contains("Unpaid"));
        Assert.Contains(options, o => o.TextContent.Contains("Partially Paid"));
        Assert.Contains(options, o => o.TextContent.Contains("Paid"));
        Assert.Contains(options, o => o.TextContent.Contains("Refunded"));
    }

    [Fact]
    public async Task Preloads_Existing_Notes_In_Form()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(notes: "Customer requested early check-in");
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var notesTextarea = cut.Find("#notes");
        var value = notesTextarea.GetAttribute("value") ?? notesTextarea.TextContent;
        Assert.Contains("Customer requested early check-in", value);
    }

    [Fact]
    public async Task Renders_Discount_Type_Dropdown_With_All_Options()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var discountTypeSelect = cut.Find("#discountType");
        var options = discountTypeSelect.QuerySelectorAll("option");
        Assert.Equal(3, options.Length);

        Assert.Contains(options, o => o.TextContent.Contains("No Discount"));
        Assert.Contains(options, o => o.TextContent.Contains("Percentage"));
        Assert.Contains(options, o => o.TextContent.Contains("Absolute Amount"));
    }

    [Fact]
    public async Task Does_Not_Show_Discount_Fields_When_Type_Is_None()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(discountType: DiscountTypeDto.None);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var html = cut.Markup;
        Assert.DoesNotContain("discountAmount", html);
        Assert.DoesNotContain("discountReason", html);
    }

    [Fact]
    public async Task Shows_Discount_Amount_Field_When_Percentage_Selected()
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
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var label = cut.Find("label[for='discountAmount']");
        Assert.Contains("Discount Percentage", label.TextContent);

        var formText = cut.Find("#discountAmount + .form-text");
        Assert.Contains("between 0 and 100", formText.TextContent);

        var discountReason = cut.Find("#discountReason");
        var reasonValue = discountReason.GetAttribute("value") ?? discountReason.TextContent;
        Assert.Contains("Early bird discount", reasonValue);
    }

    [Fact]
    public async Task Shows_Discount_Amount_Field_When_Absolute_Selected()
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
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var label = cut.Find("label[for='discountAmount']");
        Assert.Contains("Discount Amount", label.TextContent);
        Assert.DoesNotContain("Percentage", label.TextContent);
    }

    [Fact]
    public async Task Renders_Cancel_Button_With_Link_To_Bookings()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var cancelLink = cut.Find("a.btn.btn-secondary");
        Assert.Equal("/bookings", cancelLink.GetAttribute("href"));
        Assert.Contains("Cancel", cancelLink.TextContent);
    }

    [Fact]
    public async Task Submit_Button_Shows_Default_Text_Initially()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var submitButton = cut.Find("button[type='submit']");
        Assert.Contains("Update Booking", submitButton.TextContent);
        Assert.False(submitButton.HasAttribute("disabled"));
    }

    [Fact]
    public async Task Shows_Success_Message_After_Successful_Update()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto(notes: "Original notes");
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.InvokeAsync(() => Task.Delay(10));

        // Act - Update notes
        var notesTextarea = cut.Find("#notes");
        await cut.InvokeAsync(() => notesTextarea.Change("Updated notes"));

        var form = cut.Find("form");
        await cut.InvokeAsync(async () => await form.SubmitAsync());
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var successAlert = cut.Find(".alert.alert-success");
        Assert.Contains("Booking updated successfully!", successAlert.TextContent);
    }

    [Fact]
    public async Task Shows_Pending_Redirect_Message_After_Success()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.InvokeAsync(() => Task.Delay(10));

        // Act - Submit form
        var form = cut.Find("form");
        await cut.InvokeAsync(async () => await form.SubmitAsync());
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var redirectAlert = cut.Find(".alert.alert-info");
        Assert.Contains("Redirecting to bookings page in 3 seconds", redirectAlert.TextContent);

        var cancelButton = cut.Find(".alert.alert-info button");
        Assert.Contains("Cancel", cancelButton.TextContent);
    }

    [Fact]
    public async Task Cancel_Redirect_Button_Shows_Success_Message()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.InvokeAsync(() => Task.Delay(10));

        // Act - Submit and then cancel redirect
        var form = cut.Find("form");
        await cut.InvokeAsync(async () => await form.SubmitAsync());
        await cut.InvokeAsync(() => Task.Delay(10));

        var cancelButton = cut.Find(".alert.alert-info button");
        await cut.InvokeAsync(() => cancelButton.Click());

        // Assert
        var successAlert = cut.Find(".alert.alert-success");
        Assert.Contains("Booking updated successfully!", successAlert.TextContent);

        var goToBookingsButton = cut.Find(".alert.alert-success button");
        Assert.Contains("Go to Bookings", goToBookingsButton.TextContent);
    }

    [Fact]
    public async Task Buttons_Are_Disabled_During_Submission()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.InvokeAsync(() => Task.Delay(10));

        // Act - Start submission
        var form = cut.Find("form");
        var submitTask = cut.InvokeAsync(async () => await form.SubmitAsync());

        // Assert - During submission
        var submitButton = cut.Find("button[type='submit']");
        Assert.True(submitButton.HasAttribute("disabled"));
        Assert.Contains("Updating...", submitButton.TextContent);

        var spinner = submitButton.QuerySelector(".spinner-border");
        Assert.NotNull(spinner);

        var cancelLink = cut.Find("a.btn.btn-secondary");
        Assert.Contains("disabled", cancelLink.GetAttribute("class"));

        await submitTask;
    }

    [Fact]
    public async Task Form_Has_DataAnnotationsValidator()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var validator = cut.FindComponent<DataAnnotationsValidator>();
        Assert.NotNull(validator);
    }

    [Fact]
    public async Task Form_Has_ValidationSummary()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var validationSummary = cut.FindComponent<ValidationSummary>();
        Assert.NotNull(validationSummary);
    }

    [Fact]
    public async Task Displays_Discount_Reason_Help_Text()
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
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var helpText = cut.Find("#discountReason + .form-text");
        Assert.Contains("Required for audit purposes", helpText.TextContent);
        Assert.Contains("10-500 characters", helpText.TextContent);
    }

    [Fact]
    public async Task Page_Has_Correct_Title()
    {
        // Arrange
        var booking = DtoBuilders.BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(p => p.Id, booking.Id));
        await cut.InvokeAsync(() => Task.Delay(10));

        // Assert
        var pageTitle = cut.Find("h1");
        Assert.Equal("Edit Booking", pageTitle.TextContent.Trim());
    }
}
