namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SmokeCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.IntegrationScope)]
[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.BookingsArea)]
public class BookingTests(ApiFixture fixture)
{
    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
    public async Task All_booking_read_endpoints_return_persisted_room_and_bike_selections()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour("booking-details-readback", "Booking details readback", cancellationToken);
        var principal = await fixture.Client.CreateTestCustomer("Booking", "Principal", cancellationToken);
        var companion = await fixture.Client.CreateTestCustomer("Booking", "Companion", cancellationToken);
        var soloCustomer = await fixture.Client.CreateTestCustomer("Booking", "Solo", cancellationToken);
        var doubleRoomRequest = DtoBuilders.BuildCreateBookingDto(
            tourId: tour.Id,
            principalCustomerId: principal.Id,
            principalBikeType: BikeTypeDto.EBike,
            companionCustomerId: companion.Id,
            companionBikeType: BikeTypeDto.EBike,
            roomType: RoomTypeDto.DoubleOccupancy);
        var singleRoomRequest = DtoBuilders.BuildCreateBookingDto(
            tourId: tour.Id,
            principalCustomerId: soloCustomer.Id,
            principalBikeType: BikeTypeDto.EBike,
            roomType: RoomTypeDto.SingleOccupancy);
        var doubleRoomBooking = await fixture.Client.CreateBookingAndRead(doubleRoomRequest, cancellationToken);
        var singleRoomBooking = await fixture.Client.CreateBookingAndRead(singleRoomRequest, cancellationToken);

        // Act
        var doubleById = await fixture.Client.GetBookingAndRead(doubleRoomBooking.Id, cancellationToken);
        var singleById = await fixture.Client.GetBookingAndRead(singleRoomBooking.Id, cancellationToken);
        var allBookings = await fixture.Client.GetAllBookingsAndRead(cancellationToken);
        var tourBookings = await fixture.Client.GetBookingsByTourAndRead(tour.Id, cancellationToken);
        var doubleByPrincipal = (await fixture.Client.GetBookingsByCustomerAndRead(principal.Id, cancellationToken))
            .ShouldHaveSingleItem(booking => booking.Id == doubleRoomBooking.Id);
        var doubleByCompanion = (await fixture.Client.GetBookingsByCustomerAndRead(companion.Id, cancellationToken))
            .ShouldHaveSingleItem(booking => booking.Id == doubleRoomBooking.Id);
        var singleByCustomer = (await fixture.Client.GetBookingsByCustomerAndRead(soloCustomer.Id, cancellationToken))
            .ShouldHaveSingleItem(booking => booking.Id == singleRoomBooking.Id);

        // Assert
        var doubleRoomReadbacks = new[]
        {
            doubleById,
            allBookings.ShouldHaveSingleItem(booking => booking.Id == doubleRoomBooking.Id),
            tourBookings.ShouldHaveSingleItem(booking => booking.Id == doubleRoomBooking.Id),
            doubleByPrincipal,
            doubleByCompanion
        };
        doubleRoomReadbacks.ShouldAllSatisfy(booking =>
        {
            booking.RoomType.ShouldBe(RoomTypeDto.DoubleOccupancy);
            booking.PrincipalBikeType.ShouldBe(BikeTypeDto.EBike);
            booking.CompanionBikeType.ShouldBe(BikeTypeDto.EBike);
        });
        var singleRoomReadbacks = new[]
        {
            singleById,
            allBookings.ShouldHaveSingleItem(booking => booking.Id == singleRoomBooking.Id),
            tourBookings.ShouldHaveSingleItem(booking => booking.Id == singleRoomBooking.Id),
            singleByCustomer
        };
        singleRoomReadbacks.ShouldAllSatisfy(booking =>
        {
            booking.RoomType.ShouldBe(RoomTypeDto.SingleOccupancy);
            booking.PrincipalBikeType.ShouldBe(BikeTypeDto.EBike);
            booking.CompanionBikeType.ShouldBeNull();
        });
    }

    [Fact]
    public async Task Can_getbookings_smoke()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await fixture.Client.GetAsync(new Uri("/api/v1/bookings", UriKind.Relative), cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Exposes_the_apphost_managed_baseuri_through_the_host_seam()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var baseUri = fixture.BaseUri;
        var response = await fixture.Client.GetAsync(new Uri("/api/v1/bookings", UriKind.Relative), cancellationToken);

        // Assert
        (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps).ShouldBeTrue();
        string.IsNullOrWhiteSpace(baseUri.Host).ShouldBeFalse();
        (baseUri.Port > 0).ShouldBeTrue();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

}
