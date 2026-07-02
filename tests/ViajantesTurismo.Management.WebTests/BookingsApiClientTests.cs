using Microsoft.AspNetCore.Http;

namespace ViajantesTurismo.Management.WebTests;

public sealed class BookingsApiClientTests
{
    [Fact]
    public async Task GetAllBookings_requests_bookings_endpoint_and_skips_null_items()
    {
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            return CatalogToursApiClientTestsHelpers.JsonResponse($"[{AdminApiClientTestsHelpers.BookingJson}, null]");
        });
        var sut = new BookingsApiClient(httpClient);

        var bookings = await sut.GetAllBookings(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("/bookings", requestPath);
        var booking = Assert.Single(bookings);
        Assert.Equal("TOUR-1", booking.TourIdentifier);
    }

    [Fact]
    public async Task GetBookingById_returns_booking_when_admin_api_returns_success()
    {
        var requestPath = string.Empty;
        var bookingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            return CatalogToursApiClientTestsHelpers.JsonResponse(AdminApiClientTestsHelpers.BookingJson);
        });
        var sut = new BookingsApiClient(httpClient);

        var booking = await sut.GetBookingById(bookingId, Xunit.TestContext.Current.CancellationToken);

        Assert.NotNull(booking);
        Assert.Equal("/bookings/11111111-1111-1111-1111-111111111111", requestPath);
        Assert.Equal("Ada Lovelace", booking.CustomerName);
    }

    [Fact]
    public async Task GetBookingById_returns_null_when_admin_api_returns_not_found()
    {
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        var sut = new BookingsApiClient(httpClient);

        var booking = await sut.GetBookingById(Guid.CreateVersion7(), Xunit.TestContext.Current.CancellationToken);

        Assert.Null(booking);
    }

    [Theory]
    [InlineData("tour", "/bookings/tour/22222222-2222-2222-2222-222222222222")]
    [InlineData("customer", "/bookings/customer/22222222-2222-2222-2222-222222222222")]
    public async Task GetBookingsByOwner_requests_expected_endpoint(string ownerKind, string expectedPath)
    {
        var requestPath = string.Empty;
        var ownerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            return CatalogToursApiClientTestsHelpers.JsonResponse($"[{AdminApiClientTestsHelpers.BookingJson}]");
        });
        var sut = new BookingsApiClient(httpClient);

        var bookings = ownerKind == "tour"
            ? await sut.GetBookingsByTourId(ownerId, Xunit.TestContext.Current.CancellationToken)
            : await sut.GetBookingsByCustomerId(ownerId, Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(expectedPath, requestPath);
        Assert.Single(bookings);
    }

    [Fact]
    public async Task CreateBooking_posts_booking_and_returns_location()
    {
        var requestPath = string.Empty;
        var requestMethod = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            requestMethod = request.Method;
            return new HttpResponseMessage(System.Net.HttpStatusCode.Created)
            {
                Headers = { Location = new Uri("/bookings/11111111-1111-1111-1111-111111111111", UriKind.Relative) }
            };
        });
        var sut = new BookingsApiClient(httpClient);

        var location = await sut.CreateBooking(AdminApiClientTestsHelpers.CreateBooking(), Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethods.Post, requestMethod);
        Assert.Equal("/bookings", requestPath);
        Assert.Equal("/bookings/11111111-1111-1111-1111-111111111111", location.ToString());
    }

    [Theory]
    [InlineData("discount", "PUT", "/bookings/11111111-1111-1111-1111-111111111111/discount")]
    [InlineData("details", "PUT", "/bookings/11111111-1111-1111-1111-111111111111/details")]
    [InlineData("notes", "PATCH", "/bookings/11111111-1111-1111-1111-111111111111/notes")]
    public async Task UpdateBooking_sends_expected_request(string updateKind, string expectedMethod, string expectedPath)
    {
        var requestPath = string.Empty;
        var requestMethod = string.Empty;
        var bookingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            requestMethod = request.Method;
            return new HttpResponseMessage(System.Net.HttpStatusCode.NoContent);
        });
        var sut = new BookingsApiClient(httpClient);

        if (updateKind == "discount")
        {
            await sut.UpdateBookingDiscount(bookingId, AdminApiClientTestsHelpers.UpdateBookingDiscount(), Xunit.TestContext.Current.CancellationToken);
        }
        else if (updateKind == "details")
        {
            await sut.UpdateBookingDetails(bookingId, AdminApiClientTestsHelpers.UpdateBookingDetails(), Xunit.TestContext.Current.CancellationToken);
        }
        else
        {
            await sut.UpdateBookingNotes(bookingId, new UpdateBookingNotesDto { Notes = "Needs window seat" }, Xunit.TestContext.Current.CancellationToken);
        }

        Assert.Equal(expectedMethod, requestMethod);
        Assert.Equal(expectedPath, requestPath);
    }

    [Theory]
    [InlineData("cancel", "/bookings/11111111-1111-1111-1111-111111111111/cancel")]
    [InlineData("confirm", "/bookings/11111111-1111-1111-1111-111111111111/confirm")]
    [InlineData("complete", "/bookings/11111111-1111-1111-1111-111111111111/complete")]
    public async Task Booking_command_posts_expected_request(string command, string expectedPath)
    {
        var requestPath = string.Empty;
        var requestMethod = string.Empty;
        var bookingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            requestMethod = request.Method;
            return new HttpResponseMessage(System.Net.HttpStatusCode.NoContent);
        });
        var sut = new BookingsApiClient(httpClient);

        if (command == "cancel")
        {
            await sut.CancelBooking(bookingId, Xunit.TestContext.Current.CancellationToken);
        }
        else if (command == "confirm")
        {
            await sut.ConfirmBooking(bookingId, Xunit.TestContext.Current.CancellationToken);
        }
        else
        {
            await sut.CompleteBooking(bookingId, Xunit.TestContext.Current.CancellationToken);
        }

        Assert.Equal(HttpMethods.Post, requestMethod);
        Assert.Equal(expectedPath, requestPath);
    }

    [Fact]
    public async Task DeleteBooking_sends_delete_request()
    {
        var requestPath = string.Empty;
        var requestMethod = string.Empty;
        var bookingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            requestMethod = request.Method;
            return new HttpResponseMessage(System.Net.HttpStatusCode.NoContent);
        });
        var sut = new BookingsApiClient(httpClient);

        await sut.DeleteBooking(bookingId, Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethods.Delete, requestMethod);
        Assert.Equal("/bookings/11111111-1111-1111-1111-111111111111", requestPath);
    }

    [Fact]
    public async Task RecordPayment_posts_payment_and_returns_location()
    {
        var requestPath = string.Empty;
        var requestMethod = string.Empty;
        var bookingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            requestMethod = request.Method;
            return new HttpResponseMessage(System.Net.HttpStatusCode.Created)
            {
                Headers = { Location = new Uri("/bookings/11111111-1111-1111-1111-111111111111/payments/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", UriKind.Relative) }
            };
        });
        var sut = new BookingsApiClient(httpClient);

        var location = await sut.RecordPayment(bookingId, AdminApiClientTestsHelpers.CreatePayment(), Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethods.Post, requestMethod);
        Assert.Equal("/bookings/11111111-1111-1111-1111-111111111111/payments", requestPath);
        Assert.Equal("/bookings/11111111-1111-1111-1111-111111111111/payments/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", location.ToString());
    }
}
