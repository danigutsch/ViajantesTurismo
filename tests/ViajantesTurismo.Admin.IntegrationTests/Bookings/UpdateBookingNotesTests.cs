using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Helpers;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class UpdateBookingNotesTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Update_Booking()
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
        var updateRequest = DtoBuilders.BuildUpdateBookingNotesDto("Test");

        // Act
        var response = await Client.PatchAsJsonAsync(new Uri("/bookings/99999/notes", UriKind.Relative), updateRequest,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
