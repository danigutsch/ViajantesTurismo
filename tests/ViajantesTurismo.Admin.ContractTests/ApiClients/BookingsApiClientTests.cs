using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using HttpOutcomeKind = PandaBear.HttpClients.HttpOutcomeKind;
using CatalogToursApiClientTestsHelpers = PandaBear.Testing.Contracts.ContractHttpClientTestHelper;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.Admin.ContractTests.ApiClients;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, Infrastructure.TestTraits.ContractCategory)]
public sealed class BookingsApiClientTests
{
    [Fact]
    public async Task GetAllBookings_requests_bookings_endpoint_and_skips_null_items()
    {
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            return CatalogToursApiClientTestsHelpers.JsonResponse($"[{AdminApiClientTestsHelpers.BookingJson}, null]");
        });
        var sut = new BookingsApiClient(httpClient);

        var outcome = await sut.GetAllBookings(TestContext.Current.CancellationToken);

        requestPath.ShouldBe("/api/v1/bookings");
        var bookings = outcome.Value.ShouldNotBeNull();
        var booking = bookings.ShouldHaveSingleItem();
        booking.TourIdentifier.ShouldBe("TOUR-1");
    }

    [Fact]
    public async Task GetBookingById_returns_booking_when_admin_api_returns_success()
    {
        var requestPath = string.Empty;
        var bookingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            return CatalogToursApiClientTestsHelpers.JsonResponse(AdminApiClientTestsHelpers.BookingJson);
        });
        var sut = new BookingsApiClient(httpClient);

        var outcome = await sut.GetBookingById(bookingId, TestContext.Current.CancellationToken);

        var booking = outcome.Value.ShouldNotBeNull();
        requestPath.ShouldBe("/api/v1/bookings/11111111-1111-1111-1111-111111111111");
        booking.CustomerName.ShouldBe("Ada Lovelace");
        booking.RoomType.ShouldBe(RoomTypeDto.DoubleOccupancy);
        booking.PrincipalBikeType.ShouldBe(BikeTypeDto.Regular);
        booking.CompanionBikeType.ShouldBeNull();
    }

    [Fact]
    public async Task GetBookingById_returns_null_when_admin_api_returns_not_found()
    {
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        var sut = new BookingsApiClient(httpClient);

        var outcome = await sut.GetBookingById(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        outcome.Kind.ShouldBe(HttpOutcomeKind.NotFound);
        outcome.Value.ShouldBeNull();
    }

    [Fact]
    public async Task GetBookingById_returns_malformed_outcome_when_admin_api_returns_success_with_null_body()
    {
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => CatalogToursApiClientTestsHelpers.JsonResponse("null"));
        var sut = new BookingsApiClient(httpClient);

        var outcome = await sut.GetBookingById(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        outcome.Kind.ShouldBe(HttpOutcomeKind.MalformedBody);
        outcome.Value.ShouldBeNull();
    }

    [Theory]
    [InlineData("tour", "/api/v1/bookings/tour/22222222-2222-2222-2222-222222222222")]
    [InlineData("customer", "/api/v1/bookings/customer/22222222-2222-2222-2222-222222222222")]
    public async Task GetBookingsByOwner_requests_expected_endpoint(string ownerKind, string expectedPath)
    {
        var requestPath = string.Empty;
        var ownerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            return CatalogToursApiClientTestsHelpers.JsonResponse($"[{AdminApiClientTestsHelpers.BookingJson}]");
        });
        var sut = new BookingsApiClient(httpClient);

        var outcome = ownerKind == "tour"
            ? await sut.GetBookingsByTourId(ownerId, TestContext.Current.CancellationToken)
            : await sut.GetBookingsByCustomerId(ownerId, TestContext.Current.CancellationToken);

        requestPath.ShouldBe(expectedPath);
        var bookings = outcome.Value.ShouldNotBeNull();
        bookings.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task CreateBooking_posts_booking_and_returns_success_outcome()
    {
        // Arrange
        var requestPath = string.Empty;
        var requestMethod = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            requestMethod = request.Method.Method;
            return new HttpResponseMessage(System.Net.HttpStatusCode.Created)
            {
                Headers = { Location = new Uri("/api/v1/bookings/11111111-1111-1111-1111-111111111111", UriKind.Relative) }
            };
        });
        var sut = new BookingsApiClient(httpClient);

        // Act
        var outcome = await sut.CreateBooking(AdminApiClientTestsHelpers.CreateBooking(), TestContext.Current.CancellationToken);

        // Assert
        requestMethod.ShouldBe(HttpMethods.Post);
        requestPath.ShouldBe("/api/v1/bookings");
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        outcome.Location.ShouldNotBeNull();
        outcome.Location.ToString().ShouldBe("/api/v1/bookings/11111111-1111-1111-1111-111111111111");
    }

    [Fact]
    public async Task CreateBooking_returns_validation_problem_outcome()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
            CatalogToursApiClientTestsHelpers.JsonResponse(
                """
                {"errors":{"TourId":["The selected tour was not found."]}}
                """,
                System.Net.HttpStatusCode.BadRequest));
        var sut = new BookingsApiClient(httpClient);

        // Act
        var outcome = await sut.CreateBooking(AdminApiClientTestsHelpers.CreateBooking(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.ValidationProblem);
        outcome.ValidationErrors.ShouldNotBeNull();
        outcome.ValidationErrors["TourId"][0].ShouldBe("The selected tour was not found.");
    }

    [Fact]
    public async Task CreateBooking_returns_status_outcome_for_conflict()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(System.Net.HttpStatusCode.Conflict));
        var sut = new BookingsApiClient(httpClient);

        // Act
        var outcome = await sut.CreateBooking(AdminApiClientTestsHelpers.CreateBooking(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.Conflict);
        outcome.StatusCode.ShouldBe(System.Net.HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("discount", "PUT", "/api/v1/bookings/11111111-1111-1111-1111-111111111111/discount")]
    [InlineData("details", "PUT", "/api/v1/bookings/11111111-1111-1111-1111-111111111111/details")]
    [InlineData("notes", "PATCH", "/api/v1/bookings/11111111-1111-1111-1111-111111111111/notes")]
    public async Task UpdateBooking_sends_expected_request(string updateKind, string expectedMethod, string expectedPath)
    {
        var requestPath = string.Empty;
        var requestMethod = string.Empty;
        var bookingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            requestMethod = request.Method.Method;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });
        var sut = new BookingsApiClient(httpClient);

        if (updateKind == "discount")
        {
            var outcome = await sut.UpdateBookingDiscount(bookingId, AdminApiClientTestsHelpers.UpdateBookingDiscount(), TestContext.Current.CancellationToken);
            outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        }
        else if (updateKind == "details")
        {
            var outcome = await sut.UpdateBookingDetails(bookingId, AdminApiClientTestsHelpers.UpdateBookingDetails(), TestContext.Current.CancellationToken);
            outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        }
        else
        {
            var outcome = await sut.UpdateBookingNotes(bookingId, new UpdateBookingNotesDto { Notes = "Needs window seat" }, TestContext.Current.CancellationToken);
            outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        }

        requestMethod.ShouldBe(expectedMethod);
        requestPath.ShouldBe(expectedPath);
    }

    [Theory]
    [InlineData("cancel", "/api/v1/bookings/11111111-1111-1111-1111-111111111111/cancel")]
    [InlineData("confirm", "/api/v1/bookings/11111111-1111-1111-1111-111111111111/confirm")]
    [InlineData("complete", "/api/v1/bookings/11111111-1111-1111-1111-111111111111/complete")]
    public async Task Booking_command_posts_expected_request(string command, string expectedPath)
    {
        var requestPath = string.Empty;
        var requestMethod = string.Empty;
        var bookingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            requestMethod = request.Method.Method;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });
        var sut = new BookingsApiClient(httpClient);

        if (command == "cancel")
        {
            var outcome = await sut.CancelBooking(bookingId, TestContext.Current.CancellationToken);
            outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        }
        else if (command == "confirm")
        {
            var outcome = await sut.ConfirmBooking(bookingId, TestContext.Current.CancellationToken);
            outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        }
        else
        {
            var outcome = await sut.CompleteBooking(bookingId, TestContext.Current.CancellationToken);
            outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        }

        requestMethod.ShouldBe(HttpMethods.Post);
        requestPath.ShouldBe(expectedPath);
    }

    [Fact]
    public async Task DeleteBooking_sends_delete_request()
    {
        var requestPath = string.Empty;
        var requestMethod = string.Empty;
        var bookingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            requestMethod = request.Method.Method;
            return new HttpResponseMessage(System.Net.HttpStatusCode.NoContent);
        });
        var sut = new BookingsApiClient(httpClient);

        var outcome = await sut.DeleteBooking(bookingId, TestContext.Current.CancellationToken);

        requestMethod.ShouldBe(HttpMethods.Delete);
        requestPath.ShouldBe("/api/v1/bookings/11111111-1111-1111-1111-111111111111");
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
    }

    [Fact]
    public async Task RecordPayment_posts_payment_and_returns_success_outcome()
    {
        // Arrange
        var requestPath = string.Empty;
        var requestMethod = string.Empty;
        var bookingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            requestMethod = request.Method.Method;
            return new HttpResponseMessage(System.Net.HttpStatusCode.Created)
            {
                Headers = { Location = new Uri("/api/v1/bookings/11111111-1111-1111-1111-111111111111/payments/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", UriKind.Relative) }
            };
        });
        var sut = new BookingsApiClient(httpClient);

        // Act
        var outcome = await sut.RecordPayment(bookingId, AdminApiClientTestsHelpers.CreatePayment(), TestContext.Current.CancellationToken);

        // Assert
        requestMethod.ShouldBe(HttpMethods.Post);
        requestPath.ShouldBe("/api/v1/bookings/11111111-1111-1111-1111-111111111111/payments");
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        outcome.Location.ShouldNotBeNull();
        outcome.Location.ToString().ShouldBe("/api/v1/bookings/11111111-1111-1111-1111-111111111111/payments/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    }

    [Fact]
    public async Task RecordPayment_returns_not_found_outcome()
    {
        // Arrange
        var bookingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        var sut = new BookingsApiClient(httpClient);

        // Act
        var outcome = await sut.RecordPayment(bookingId, AdminApiClientTestsHelpers.CreatePayment(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.NotFound);
        outcome.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateBooking_logs_non_success_outcome_without_response_body()
    {
        // Arrange
        var logger = new CollectingLogger<BookingsApiClient>();
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
            CatalogToursApiClientTestsHelpers.JsonResponse("not json booking@example.test", System.Net.HttpStatusCode.BadRequest));
        var sut = new BookingsApiClient(httpClient, logger);

        // Act
        var outcome = await sut.CreateBooking(AdminApiClientTestsHelpers.CreateBooking(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.MalformedBody);
        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.LogLevel.ShouldBe(LogLevel.Warning);
        entry.Message.ShouldBe("Booking create returned BadRequest with outcome MalformedBody.");
        entry.Message.Contains("booking@example.test", StringComparison.Ordinal).ShouldBeFalse();
        entry.State.Values.Any(value => value.Contains("booking@example.test", StringComparison.Ordinal)).ShouldBeFalse();
    }
}
