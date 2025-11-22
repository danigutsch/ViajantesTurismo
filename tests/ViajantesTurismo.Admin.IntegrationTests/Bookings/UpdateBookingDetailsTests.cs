using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Helpers;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class UpdateBookingDetailsTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Update_Booking_Details_Add_Companion_And_RoomSupplement()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var principal = await Client.CreateTestCustomer("Prim", "Ary", cancellationToken: TestContext.Current.CancellationToken);
        var companion = await Client.CreateTestCustomer("Comp", "Anion", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, principal.Id, cancellationToken: TestContext.Current.CancellationToken);

        var updateDto = DtoBuilders.BuildUpdateBookingDetailsDto(RoomTypeDto.DoubleRoom, BikeTypeDto.Regular, companion.Id, BikeTypeDto.Regular);

        // Act
        var response = await Client.UpdateBookingDetails(booking.Id, updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(companion.Id, updated.CompanionId);
        var expected = PricingHelper.CalculateExpectedBookingPrice(TestDefaults.BaseTourPrice, TestDefaults.DoubleRoomSupplement, TestDefaults.RegularBikePrice, TestDefaults.RegularBikePrice);
        Assert.Equal(expected, updated.TotalPrice);
    }

    [Fact]
    public async Task Can_Update_Booking_Details_Remove_Companion_And_Switch_To_Single()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var principal = await Client.CreateTestCustomer("Switch", "Down", cancellationToken: TestContext.Current.CancellationToken);
        var companion = await Client.CreateTestCustomer("To", "Single", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, principal.Id, companion.Id, cancellationToken: TestContext.Current.CancellationToken);

        var updateDto = DtoBuilders.BuildUpdateBookingDetailsDto(RoomTypeDto.SingleRoom, BikeTypeDto.Regular);

        // Act
        var response = await Client.UpdateBookingDetails(booking.Id, updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Null(updated.CompanionId);
        var expected = PricingHelper.CalculateExpectedBookingPrice(TestDefaults.BaseTourPrice, 0m, TestDefaults.RegularBikePrice);
        Assert.Equal(expected, updated.TotalPrice);
    }

    [Fact]
    public async Task Update_Booking_Details_SingleRoomWithCompanion_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var principal = await Client.CreateTestCustomer("Bad", "Combo", cancellationToken: TestContext.Current.CancellationToken);
        var companion = await Client.CreateTestCustomer("Wrong", "Room", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, principal.Id, cancellationToken: TestContext.Current.CancellationToken);

        var updateDto = DtoBuilders.BuildUpdateBookingDetailsDto(RoomTypeDto.SingleRoom, BikeTypeDto.Regular, companion.Id, BikeTypeDto.Regular);

        // Act
        var response = await Client.UpdateBookingDetails(booking.Id, updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_Booking_Details_Returns_NotFound_For_Invalid_Id()
    {
        // Arrange
        var nonExistingId = Guid.CreateVersion7();
        var updateDto = DtoBuilders.BuildUpdateBookingDetailsDto(RoomTypeDto.SingleRoom, BikeTypeDto.Regular);

        // Act
        var response = await Client.UpdateBookingDetails(nonExistingId, updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cannot_Update_Booking_Details_With_DoubleRoom_Without_Companion()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var principal = await Client.CreateTestCustomer("Single", "Person", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, principal.Id, cancellationToken: TestContext.Current.CancellationToken);

        var updateDto = DtoBuilders.BuildUpdateBookingDetailsDto(RoomTypeDto.DoubleRoom, BikeTypeDto.Regular);

        // Act
        var response = await Client.UpdateBookingDetails(booking.Id, updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cannot_Update_Booking_Details_With_CompanionBike_Without_Companion()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var principal = await Client.CreateTestCustomer("Solo", "Rider", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, principal.Id, cancellationToken: TestContext.Current.CancellationToken);

        var updateDto = DtoBuilders.BuildUpdateBookingDetailsDto(RoomTypeDto.SingleRoom, BikeTypeDto.Regular, null, BikeTypeDto.Regular);

        // Act
        var response = await Client.UpdateBookingDetails(booking.Id, updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cannot_Update_Booking_Details_With_Companion_Without_CompanionBike()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var principal = await Client.CreateTestCustomer("Needs", "Bike", cancellationToken: TestContext.Current.CancellationToken);
        var companion = await Client.CreateTestCustomer("No", "Bike", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, principal.Id, cancellationToken: TestContext.Current.CancellationToken);

        var updateDto = DtoBuilders.BuildUpdateBookingDetailsDto(RoomTypeDto.DoubleRoom, BikeTypeDto.Regular, companion.Id);

        // Act
        var response = await Client.UpdateBookingDetails(booking.Id, updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cannot_Update_Confirmed_Booking_Details()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var principal = await Client.CreateTestCustomer("Confirmed", "Already", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, principal.Id, cancellationToken: TestContext.Current.CancellationToken);

        await Client.ConfirmBooking(booking.Id, TestContext.Current.CancellationToken);

        var updateDto = DtoBuilders.BuildUpdateBookingDetailsDto(RoomTypeDto.SingleRoom, BikeTypeDto.EBike);

        // Act
        var response = await Client.UpdateBookingDetails(booking.Id, updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
