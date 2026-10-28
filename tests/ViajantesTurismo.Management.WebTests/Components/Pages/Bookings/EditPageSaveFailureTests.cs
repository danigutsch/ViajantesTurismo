using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ViajantesTurismo.Management.Web.Components.Pages.Bookings;
using ViajantesTurismo.Management.WebTests.Infrastructure;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Bookings;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
public sealed class EditPageSaveFailureTests : BunitContext
{
    private const string UpdateFailedMessage = "We couldn't update the booking right now. Please try again.";

    private readonly FakeBookingsApiClient fakeBookingsApi = new();
    private readonly FakeCustomersApiClient fakeCustomersApi = new();
    private readonly RecordingUiFailureReporter failures = new();

    public EditPageSaveFailureTests()
    {
        Services.AddSingleton<PandaBear.Blazor.IUiFailureReporter>(failures);
        Services.AddSingleton(TimeProvider.System);
        Services.AddSingleton<IBookingsApiClient>(fakeBookingsApi);
        Services.AddSingleton<ICustomersApiClient>(fakeCustomersApi);
    }

    [Fact]
    public async Task Unknown_booking_shows_the_not_found_message_with_a_way_back()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, unknownId));
        await cut.WaitForAssertionAsync(() => cut.Find(".alert.alert-danger"));

        // Assert
        cut.Find(".alert.alert-danger").TextContent.Trim().ShouldBe("Booking not found.");
        cut.Find("a[href='/bookings']").TextContent.Trim().ShouldBe("Back to Bookings");
        cut.FindAll("form").ShouldBeEmpty();
        failures.Reports.ShouldBeEmpty();
    }

    [Fact]
    public async Task Rejected_details_update_shows_fixed_copy_skips_later_updates_and_reloads_the_booking()
    {
        // Arrange
        var booking = BuildBookingDto(notes: "Original notes", principalBikeType: BikeTypeDto.Regular);
        fakeBookingsApi.AddBooking(booking);
        fakeBookingsApi.SetUpdateBookingDetailsOutcome(FakeApiClientOutcomes.TransportFailure());
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("form"));
        await cut.Find("#principalBikeType").ChangeAsync(new ChangeEventArgs { Value = nameof(BikeTypeDto.EBike) });
        await cut.Find("#notes").ChangeAsync(new ChangeEventArgs { Value = "Attempted notes" });

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Find(".alert.alert-danger").TextContent.Trim().ShouldBe(UpdateFailedMessage);
        cut.Markup.ShouldNotContain("HTTP transport failed.", StringComparison.Ordinal);
        fakeBookingsApi.UpdateBookingDetailsCallCount.ShouldBe(1);
        fakeBookingsApi.UpdateBookingDiscountCallCount.ShouldBe(0);
        fakeBookingsApi.UpdateBookingNotesCallCount.ShouldBe(0);
        fakeBookingsApi.GetBookingByIdCallCount.ShouldBe(2);
        cut.Find("#notes").GetAttribute("value").ShouldBe("Attempted notes");
        cut.Find("button[type='submit']").HasAttribute("disabled").ShouldBeFalse();
        failures.Reports.ShouldBeEmpty();
    }

    [Fact]
    public async Task Changed_discount_is_updated_and_the_new_discount_survives_the_refresh()
    {
        // Arrange
        var booking = BuildBookingDto(discountType: DiscountTypeDto.None, discountReason: null);
        fakeBookingsApi.AddBooking(booking);
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("#discountType"));
        await cut.Find("#discountType").ChangeAsync(new ChangeEventArgs { Value = nameof(DiscountTypeDto.Percentage) });
        await cut.Find("#discountAmount").ChangeAsync(new ChangeEventArgs { Value = "10" });
        await cut.Find("#discountReason").ChangeAsync(new ChangeEventArgs { Value = "Early bird discount offer" });

        // Act
        var submitTask = cut.Find("form").SubmitAsync();
        await cut.WaitForAssertionAsync(() => cut.FindAll(".alert.alert-info").ShouldHaveSingleItem().TextContent.ShouldContain("Redirecting", StringComparison.Ordinal));

        // Assert
        fakeBookingsApi.UpdateBookingDiscountCallCount.ShouldBe(1);
        fakeBookingsApi.UpdateBookingDetailsCallCount.ShouldBe(0);
        fakeBookingsApi.UpdateBookingNotesCallCount.ShouldBe(0);
        cut.Find("#discountType").GetAttribute("value").ShouldBe(nameof(DiscountTypeDto.Percentage));
        cut.Find("#discountReason").GetAttribute("value").ShouldBe("Early bird discount offer");
        await cut.Find(".alert.alert-info button").ClickAsync();
        await submitTask;
    }

    [Fact]
    public async Task Rejected_discount_update_shows_fixed_copy_skips_the_notes_update_and_reloads_the_booking()
    {
        // Arrange
        var booking = BuildBookingDto(notes: "Original notes", discountType: DiscountTypeDto.None, discountReason: null);
        fakeBookingsApi.AddBooking(booking);
        fakeBookingsApi.SetUpdateBookingDiscountOutcome(FakeApiClientOutcomes.TransportFailure());
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("#discountType"));
        await cut.Find("#discountType").ChangeAsync(new ChangeEventArgs { Value = nameof(DiscountTypeDto.Percentage) });
        await cut.Find("#discountAmount").ChangeAsync(new ChangeEventArgs { Value = "10" });
        await cut.Find("#discountReason").ChangeAsync(new ChangeEventArgs { Value = "Early bird discount offer" });
        await cut.Find("#notes").ChangeAsync(new ChangeEventArgs { Value = "Attempted notes" });

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Find(".alert.alert-danger").TextContent.Trim().ShouldBe(UpdateFailedMessage);
        cut.Markup.ShouldNotContain("HTTP transport failed.", StringComparison.Ordinal);
        fakeBookingsApi.UpdateBookingDiscountCallCount.ShouldBe(1);
        fakeBookingsApi.UpdateBookingNotesCallCount.ShouldBe(0);
        fakeBookingsApi.GetBookingByIdCallCount.ShouldBe(2);
        cut.Find("#notes").GetAttribute("value").ShouldBe("Attempted notes");
        failures.Reports.ShouldBeEmpty();
    }

    [Fact]
    public async Task Go_to_bookings_after_cancelling_the_redirect_navigates_to_the_bookings_list()
    {
        // Arrange
        var booking = BuildBookingDto();
        fakeBookingsApi.AddBooking(booking);
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("form"));
        var submitTask = cut.Find("form").SubmitAsync();
        await cut.WaitForAssertionAsync(() => cut.Find(".alert.alert-info button"));
        await cut.Find(".alert.alert-info button").ClickAsync();
        await submitTask;

        // Act
        await cut.Find(".alert.alert-success button").ClickAsync();

        // Assert
        Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith("/bookings", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Submitting_again_while_a_save_is_in_flight_does_not_send_a_second_update()
    {
        // Arrange
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var booking = BuildBookingDto(notes: "Original notes");
        fakeBookingsApi.AddBooking(booking);
        fakeBookingsApi.SetUpdateBookingNotesGate(gate.Task);
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("form"));
        await cut.Find("#notes").ChangeAsync(new ChangeEventArgs { Value = "Updated notes" });
        var firstSubmit = cut.Find("form").SubmitAsync();
        await cut.WaitForAssertionAsync(() => fakeBookingsApi.UpdateBookingNotesCallCount.ShouldBe(1));

        // Act
        await cut.Find("form").SubmitAsync();
        gate.SetResult();
        await cut.WaitForAssertionAsync(() => cut.Find(".alert.alert-info button"));
        await cut.Find(".alert.alert-info button").ClickAsync();
        await firstSubmit;

        // Assert
        fakeBookingsApi.UpdateBookingNotesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Removing_the_companion_removes_the_companion_bike_choice()
    {
        // Arrange
        var principal = BuildCustomerDto(firstName: "Alice", lastName: "Principal");
        var companion = BuildCustomerDto(firstName: "Bob", lastName: "Companion");
        fakeCustomersApi.AddCustomer(principal);
        fakeCustomersApi.AddCustomer(companion);
        var booking = BuildBookingDto(
            customerId: principal.Id,
            companionId: companion.Id,
            companionName: "Bob Companion",
            companionBikeType: BikeTypeDto.Regular,
            roomType: RoomTypeDto.DoubleOccupancy);
        fakeBookingsApi.AddBooking(booking);
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("#companionBikeType"));

        // Act
        await cut.Find("#companionId").ChangeAsync(new ChangeEventArgs { Value = string.Empty });

        // Assert
        cut.FindAll("#companionBikeType").ShouldBeEmpty();
    }

    [Fact]
    public async Task Changed_companion_is_shown_by_name_even_when_the_refresh_after_saving_fails()
    {
        // Arrange
        var principal = BuildCustomerDto(firstName: "Alice", lastName: "Principal");
        var companion = BuildCustomerDto(firstName: "Bob", lastName: "Companion", bikeType: BikeTypeDto.Regular);
        fakeCustomersApi.AddCustomer(principal);
        fakeCustomersApi.AddCustomer(companion);
        var booking = BuildBookingDto(customerId: principal.Id, roomType: RoomTypeDto.DoubleOccupancy, principalBikeType: BikeTypeDto.Regular);
        fakeBookingsApi.AddBooking(booking);
        fakeBookingsApi.SetGetBookingByIdOutcomeOnCall(2, FakeApiClientOutcomes.TransportFailure<GetBookingDto>());
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("#companionId"));
        await cut.Find("#companionId").ChangeAsync(new ChangeEventArgs { Value = companion.Id.ToString() });

        // Act
        var submitTask = cut.Find("form").SubmitAsync();
        await cut.WaitForAssertionAsync(() => cut.Find(".alert.alert-info button"));

        // Assert
        cut.Markup.ShouldContain("The change was saved, but we couldn't refresh the booking", StringComparison.Ordinal);
        cut.Markup.ShouldContain("Bob Companion", StringComparison.Ordinal);
        fakeBookingsApi.LastUpdatedDetails.ShouldNotBeNull().CompanionCustomerId.ShouldBe(companion.Id);
        await cut.Find(".alert.alert-info button").ClickAsync();
        await submitTask;
    }

    [Fact]
    public async Task Confirming_a_status_action_while_a_save_is_in_flight_does_not_run_the_action()
    {
        // Arrange
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var booking = BuildBookingDto(status: BookingStatusDto.Confirmed, notes: "Original notes");
        fakeBookingsApi.AddBooking(booking);
        fakeBookingsApi.SetUpdateBookingNotesGate(gate.Task);
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find(".card.border-danger"));
        var cancelButton = cut.FindAll(".card.border-danger button")
            .ShouldHaveSingleItem(button => button.TextContent.Contains("Cancel Booking", StringComparison.Ordinal));
        var cancelClick = Task.Run(async () => await cancelButton.ClickAsync(new MouseEventArgs()), Xunit.TestContext.Current.CancellationToken);
        await cut.WaitForAssertionAsync(() => cut.Find(".modal.show"));
        await cut.Find("#notes").ChangeAsync(new ChangeEventArgs { Value = "Updated notes" });
        var submit = cut.Find("form").SubmitAsync();
        await cut.WaitForAssertionAsync(() => fakeBookingsApi.UpdateBookingNotesCallCount.ShouldBe(1));

        // Act
        var confirmButton = cut.FindAll(".modal.show button")
            .ShouldHaveSingleItem(button => button.TextContent.Contains("Yes, Cancel", StringComparison.Ordinal));
        await confirmButton.ClickAsync(new MouseEventArgs());
        await cancelClick;
        gate.SetResult();
        await cut.WaitForAssertionAsync(() => cut.Find(".alert.alert-info button"));
        await cut.Find(".alert.alert-info button").ClickAsync();
        await submit;

        // Assert
        fakeBookingsApi.BookingActionCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Switching_from_single_to_double_room_offers_the_companion_choice()
    {
        // Arrange
        var booking = BuildBookingDto(roomType: RoomTypeDto.SingleOccupancy);
        fakeBookingsApi.AddBooking(booking);
        var cut = Render<Edit>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("#roomType"));
        cut.FindAll("#companionId").ShouldBeEmpty();

        // Act
        await cut.Find("#roomType").ChangeAsync(new ChangeEventArgs { Value = nameof(RoomTypeDto.DoubleOccupancy) });

        // Assert
        cut.Find("label[for='companionId']").TextContent.Trim().ShouldBe("Companion (Optional)");
    }
}
