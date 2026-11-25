using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application;

/// <summary>
/// Provides query operations for the admin domain (read-only operations).
/// </summary>
/// <remarks>
/// This interface follows the CQRS (Command Query Responsibility Segregation) pattern:
/// <list type="bullet">
/// <item><description><see cref="IQueryService"/> is used for QUERY operations (Read) that retrieve data without modification.</description></item>
/// <item><description>Stores (like ITourStore, ICustomerStore) are used for COMMAND operations (Create, Update, Delete) that modify state.</description></item>
/// <item><description>Query endpoints should ONLY use <see cref="IQueryService"/> and never use stores.</description></item>
/// <item><description>Command endpoints should ONLY use stores and never use <see cref="IQueryService"/>.</description></item>
/// </list>
/// This separation allows for optimized read and write models, better scalability, and clearer separation of concerns.
/// Query methods return DTOs optimized for presentation, while stores return full aggregate roots for modification.
/// </remarks>
public interface IQueryService
{
    /// <summary>Retrieves a bounded page of active tour summaries.</summary>
    /// <param name="page">The optional one-based page number.</param>
    /// <param name="sort">The requested column.</param>
    /// <param name="descending">Whether the selected column uses descending order.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The selected tour summaries and the active total.</returns>
    Task<GetTourPageDto> GetTourPage(string? page, TourPageSort sort, bool descending, CancellationToken ct);

    /// <summary>Finds a bounded slice of active tours by name or operational identifier.</summary>
    /// <param name="request">The optional search text and requested result limit.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The matching choices and count.</returns>
    Task<GetTourOptionsDto> SearchTourOptions(TourOptionSearchDto request, CancellationToken ct);

    /// <summary>
    /// Retrieves a tour by ID, including retained historical tours.
    /// </summary>
    /// <param name="id">The tour ID.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The tour DTO or null if not found.</returns>
    Task<GetTourDto?> GetTourById(Guid id, CancellationToken ct);

    /// <summary>Retrieves an active tour by ID or null if the tour is missing or retired.</summary>
    Task<GetTourDto?> GetActiveTourById(Guid id, CancellationToken ct);

    /// <summary>Retrieves the accepted retirement for a tour or null if it has no retirement.</summary>
    Task<GetTourRetirementDto?> GetTourRetirement(Guid id, CancellationToken ct);

    /// <summary>Retrieves a bounded page of customer summaries.</summary>
    /// <param name="page">The optional one-based page number.</param>
    /// <param name="sort">The requested column.</param>
    /// <param name="descending">Whether to sort the column in descending order.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The customer summaries and total count.</returns>
    Task<GetCustomerPageDto> GetCustomerPage(string? page, CustomerPageSort sort, bool descending, CancellationToken ct);

    /// <summary>Finds a bounded slice of customer choices by name or complete identifier.</summary>
    /// <param name="request">The search text, result limit, and optional excluded customer.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The matching choices and count.</returns>
    Task<GetCustomerOptionsDto> SearchCustomerOptions(CustomerOptionSearchDto request, CancellationToken ct);

    /// <summary>
    /// Retrieves customer details by ID.
    /// </summary>
    /// <param name="id">The customer ID.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The customer details DTO or null if not found.</returns>
    Task<CustomerDetailsDto?> GetCustomerDetailsById(Guid id, CancellationToken ct);

    /// <summary>Retrieves a bounded page of summaries and overall booking counts.</summary>
    /// <param name="page">The optional one-based page number.</param>
    /// <param name="sort">The requested sort key.</param>
    /// <param name="descending">Whether to reverse the selected sort key.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The page and overview counts.</returns>
    Task<GetBookingPageDto> GetBookingPage(string? page, BookingPageSort sort, bool descending, CancellationToken ct);

    /// <summary>Retrieves a page from a tour's booking history.</summary>
    /// <param name="tourId">The booked tour.</param>
    /// <param name="page">The optional one-based page.</param>
    /// <param name="sort">The requested sort, where default puts cancelled bookings first and then newest first.</param>
    /// <param name="descending">Whether to reverse the selected sort.</param>
    /// <param name="ct">The cancellation token.</param>
    Task<GetBookingHistoryPageDto> GetTourBookingPage(Guid tourId, string? page, BookingPageSort sort, bool descending, CancellationToken ct);

    /// <summary>Retrieves a page from a customer's history as principal or companion.</summary>
    /// <param name="customerId">The customer whose bookings are returned.</param>
    /// <param name="page">The optional one-based page.</param>
    /// <param name="sort">The requested sort, where default uses the newest booking date first.</param>
    /// <param name="descending">Whether to reverse the selected sort.</param>
    /// <param name="ct">The cancellation token.</param>
    Task<GetBookingHistoryPageDto> GetCustomerBookingPage(Guid customerId, string? page, BookingPageSort sort, bool descending, CancellationToken ct);

    /// <summary>
    /// Retrieves a booking by ID.
    /// </summary>
    /// <param name="id">The booking ID.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The booking DTO or null if not found.</returns>
    Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken ct);

}
