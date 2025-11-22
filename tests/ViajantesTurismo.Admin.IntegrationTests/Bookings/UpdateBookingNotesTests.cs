using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Helpers;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class UpdateBookingNotesTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Update_Booking_Notes()
    {
        // Arrange
        var tourDto = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customerDto = await Client.CreateTestCustomer("Jack", "Martin", cancellationToken: TestContext.Current.CancellationToken);
        var createdBooking = await Client.CreateTestBooking(tourDto.Id, customerDto.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var notesRequest = DtoBuilders.BuildUpdateBookingNotesDto("Updated notes");
        var notesResponse = await Client.PatchAsJsonAsync(
            new Uri($"/bookings/{createdBooking.Id}/notes", UriKind.Relative),
            notesRequest,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, notesResponse.StatusCode);
        var updatedBooking = await notesResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updatedBooking);
        Assert.Equal("Updated notes", updatedBooking.Notes);
    }

    [Fact]
    public async Task Can_Update_Notes_And_Then_Confirm()
    {
        // Arrange
        var tourDto = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customerDto = await Client.CreateTestCustomer("Update", "Then Confirm", cancellationToken: TestContext.Current.CancellationToken);
        var createdBooking = await Client.CreateTestBooking(tourDto.Id, customerDto.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var notesRequest = DtoBuilders.BuildUpdateBookingNotesDto("Updated notes");
        var notesResponse = await Client.PatchAsJsonAsync(
            new Uri($"/bookings/{createdBooking.Id}/notes", UriKind.Relative),
            notesRequest,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, notesResponse.StatusCode);
        var updatedBooking = await notesResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updatedBooking);
        Assert.Equal("Updated notes", updatedBooking.Notes);

        var confirmResponse = await Client.PostAsync(
            new Uri($"/bookings/{createdBooking.Id}/confirm", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var confirmedBooking = await confirmResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(confirmedBooking);
        Assert.Equal("Updated notes", confirmedBooking.Notes);
        Assert.Equal(BookingStatusDto.Confirmed, confirmedBooking.Status);
    }

    [Fact]
    public async Task Update_Booking_Notes_Returns_Not_Found_For_Invalid_Id()
    {
        // Arrange
        var nonExistingId = Guid.CreateVersion7();
        var updateRequest = DtoBuilders.BuildUpdateBookingNotesDto("Test");

        // Act
        var response = await Client.UpdateBookingNotes(nonExistingId, updateRequest,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Can_Set_Notes_To_Null()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Clear", "Notes", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        // First set some notes
        var setRequest = DtoBuilders.BuildUpdateBookingNotesDto("Initial notes");
        var setResponse = await Client.UpdateBookingNotes(booking.Id, setRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);
        var withNotes = await setResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.Equal("Initial notes", withNotes!.Notes);

        // Act - Clear notes by setting to null (create DTO directly to avoid default value)
        var clearRequest = new UpdateBookingNotesDto { Notes = null };
        var response = await Client.UpdateBookingNotes(booking.Id, clearRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Null(updated.Notes);
    }

    [Fact]
    public async Task Can_Set_Notes_To_Empty_String()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Empty", "Notes", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        // First set some notes
        var setRequest = DtoBuilders.BuildUpdateBookingNotesDto("Some notes");
        var setResponse = await Client.UpdateBookingNotes(booking.Id, setRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);

        // Act
        var emptyRequest = DtoBuilders.BuildUpdateBookingNotesDto("");
        var response = await Client.UpdateBookingNotes(booking.Id, emptyRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("", updated.Notes);
    }

    [Fact]
    public async Task Can_Update_Notes_With_Long_Text()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Long", "Notes", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var longNotes = new string('A', 2000); // 2000 characters

        // Act
        var request = DtoBuilders.BuildUpdateBookingNotesDto(longNotes);
        var response = await Client.UpdateBookingNotes(booking.Id, request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(longNotes, updated.Notes);
    }
}
