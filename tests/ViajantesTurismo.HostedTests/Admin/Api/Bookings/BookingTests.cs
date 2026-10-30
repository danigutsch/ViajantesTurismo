using PandaBear.Testing;
using System.Net;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.HostedTests.Admin.Api.Infrastructure;
using ViajantesTurismo.Admin.Testing.Builders;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;
namespace ViajantesTurismo.HostedTests.Admin.Api.Bookings;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.SmokeCategory)]
[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.BookingsArea)]
[TestScope(TestScope.Integration)]
public class BookingTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
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
        var actualValue = await fixture.Client.GetBookingsByCustomerAndRead(principal.Id, cancellationToken);
        var doubleByPrincipal = actualValue
            .ShouldHaveSingleItem(booking => booking.Id == doubleRoomBooking.Id);
        var actualValue1 = await fixture.Client.GetBookingsByCustomerAndRead(companion.Id, cancellationToken);
        var doubleByCompanion = actualValue1
            .ShouldHaveSingleItem(booking => booking.Id == doubleRoomBooking.Id);
        var actualValue2 = await fixture.Client.GetBookingsByCustomerAndRead(soloCustomer.Id, cancellationToken);
        var singleByCustomer = actualValue2
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
        var areEqual = baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps;
        areEqual.ShouldBeTrue();
        string.IsNullOrWhiteSpace(baseUri.Host).ShouldBeFalse();
        var isValid = baseUri.Port > 0;
        isValid.ShouldBeTrue();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

}
