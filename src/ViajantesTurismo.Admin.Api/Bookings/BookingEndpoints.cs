using ViajantesTurismo.Admin.Api.Bookings.CancelBooking;
using ViajantesTurismo.Admin.Api.Bookings.CompleteBooking;
using ViajantesTurismo.Admin.Api.Bookings.ConfirmBooking;
using ViajantesTurismo.Admin.Api.Bookings.CreateBooking;
using ViajantesTurismo.Admin.Api.Bookings.DeleteBooking;
using ViajantesTurismo.Admin.Api.Bookings.UpdateBookingDetails;
using ViajantesTurismo.Admin.Api.Bookings.UpdateBookingDiscount;
using ViajantesTurismo.Admin.Api.Bookings.UpdateBookingNotes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Api.Bookings;

/// <summary>
/// Defines all endpoints related to booking management.
/// </summary>
internal static class BookingEndpoints
{
    private const string ListEndpoint = nameof(GetBookings);
    private const string ItemEndpoint = nameof(GetBookingById);

    /// <summary>
    /// Maps all booking-related endpoints to the application.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    /// <returns>The web application for chaining.</returns>
    public static void MapBookingEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var bookingsGroup = app.MapBookingsGroup();

        bookingsGroup.MapGet("/", GetBookings)
            .RequireAuthorization(AdminAuthorization.BookingRead, AdminAuthorization.PaymentRead)
            .WithAdminMetadata(ListEndpoint, "Retrieves all bookings.", "Retrieves all bookings.");

        bookingsGroup.MapGet("/page", GetBookingPage)
            .RequireAuthorization(AdminAuthorization.BookingRead, AdminAuthorization.PaymentRead)
            .WithAdminMetadata(nameof(GetBookingPage), "Retrieves a page of bookings.",
                "Retrieves bounded booking summaries and the overview counts. The optional page number is one-based.");

        bookingsGroup.MapGet("/{id:guid}", GetBookingById)
            .RequireAuthorization(AdminAuthorization.BookingRead, AdminAuthorization.PaymentRead)
            .WithAdminMetadata(ItemEndpoint, "Retrieves a booking by its ID.", "Retrieves a booking by its ID.");

        bookingsGroup.MapGet("/tour/{tourId:guid}", GetBookingsByTourId)
            .RequireAuthorization(AdminAuthorization.BookingRead, AdminAuthorization.PaymentRead)
            .WithAdminMetadata("GetBookingsByTourId", "Retrieves all bookings for a specific tour.", "Retrieves all bookings for a specific tour.");

        bookingsGroup.MapGet("/customer/{customerId:guid}", GetBookingsByCustomerId)
            .RequireAuthorization(AdminAuthorization.BookingRead, AdminAuthorization.PaymentRead)
            .WithAdminMetadata("GetBookingsByCustomerId", "Retrieves all bookings for a specific customer (as primary or companion).", "Retrieves all bookings for a specific customer.");

        bookingsGroup.MapCreateBookingEndpoint();

        bookingsGroup.MapUpdateBookingDiscountEndpoint();
        bookingsGroup.MapUpdateBookingDetailsEndpoint();
        bookingsGroup.MapDeleteBookingEndpoint();
        bookingsGroup.MapCancelBookingEndpoint();
        bookingsGroup.MapConfirmBookingEndpoint();
        bookingsGroup.MapUpdateBookingNotesEndpoint();
        bookingsGroup.MapCompleteBookingEndpoint();
    }

    private static async Task<Ok<IReadOnlyList<GetBookingDto>>> GetBookings(
        [FromServices] IQueryService queryService,
        LinkGenerator linkGenerator,
        HttpContext context,
        CancellationToken ct)
    {
        var bookings = await queryService.GetAllBookings(ct);
        return TypedResults.Ok(WithResourceLinks(bookings, context, linkGenerator));
    }

    private static async Task<Results<Ok<GetBookingPageDto>, BadRequest>> GetBookingPage(
        [FromQuery] string? page,
        [FromQuery] string? sort,
        [FromQuery] bool? descending,
        [FromServices] IQueryService queryService,
        LinkGenerator linkGenerator,
        HttpContext context,
        CancellationToken ct)
    {
        var sortKey = BookingPageSort.Default;
        if (sort is not null && (!Enum.TryParse(sort, ignoreCase: true, out sortKey) || !Enum.IsDefined(sortKey)))
        {
            return TypedResults.BadRequest();
        }

        var bookings = await queryService.GetBookingPage(page, sortKey, descending.GetValueOrDefault(), ct);
        return TypedResults.Ok(bookings with
        {
            Items = WithResourceLinks(bookings.Items, context, linkGenerator)
        });
    }

    private static async Task<Results<Ok<GetBookingDto>, NotFound<ProblemDetails>>> GetBookingById(
        [FromRoute] Guid id,
        [FromServices] IQueryService queryService,
        LinkGenerator linkGenerator,
        HttpContext context,
        CancellationToken ct)
    {
        var booking = await queryService.GetBookingById(id, ct);
        if (booking is null)
        {
            return BookingErrors.BookingNotFound().ToNotFound();
        }

        return TypedResults.Ok(booking with
        {
            Links = GetResourceLinks(context, linkGenerator).For(id).Links
        });
    }

    private static async Task<Ok<IReadOnlyList<GetBookingDto>>> GetBookingsByTourId(
        [FromRoute] Guid tourId,
        [FromServices] IQueryService queryService,
        LinkGenerator linkGenerator,
        HttpContext context,
        CancellationToken ct)
    {
        var bookings = await queryService.GetBookingsByTourId(tourId, ct);
        return TypedResults.Ok(WithResourceLinks(bookings, context, linkGenerator));
    }

    private static async Task<Ok<IReadOnlyList<GetBookingDto>>> GetBookingsByCustomerId(
        [FromRoute] Guid customerId,
        [FromServices] IQueryService queryService,
        LinkGenerator linkGenerator,
        HttpContext context,
        CancellationToken ct)
    {
        var bookings = await queryService.GetBookingsByCustomerId(customerId, ct);
        return TypedResults.Ok(WithResourceLinks(bookings, context, linkGenerator));
    }

    private static IReadOnlyList<GetBookingDto> WithResourceLinks(
        IReadOnlyList<GetBookingDto> bookings, HttpContext context, LinkGenerator linkGenerator)
    {
        var links = GetResourceLinks(context, linkGenerator);
        return [.. bookings.Select(booking => booking with { Links = links.For(booking.Id).Links })];
    }

    /// <summary>Creates the link factory for booking representations.</summary>
    internal static AdminResourceLinks GetResourceLinks(HttpContext context, LinkGenerator linkGenerator) =>
        new(context, linkGenerator, ItemEndpoint, ListEndpoint);
}
