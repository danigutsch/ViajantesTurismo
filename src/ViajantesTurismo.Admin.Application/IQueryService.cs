using ViajantesTurismo.Admin.Contracts;

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
    /// <summary>
    /// Retrieves all tours sorted by ID.
    /// </summary>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A read-only list of <see cref="GetTourDto"/> representing all tours.</returns>
    Task<IReadOnlyList<GetTourDto>> GetAllTours(CancellationToken ct);

    /// <summary>
    /// Retrieves a tour by ID.
    /// </summary>
    /// <param name="id">The tour ID.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The tour DTO or null if not found.</returns>
    Task<GetTourDto?> GetTourById(Guid id, CancellationToken ct);

    /// <summary>
    /// Retrieves all customers.
    /// </summary>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A read-only list of customer DTOs.</returns>
    Task<IReadOnlyList<GetCustomerDto>> GetAllCustomers(CancellationToken ct);

    /// <summary>
    /// Retrieves customer details by ID.
    /// </summary>
    /// <param name="id">The customer ID.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The customer details DTO or null if not found.</returns>
    Task<CustomerDetailsDto?> GetCustomerDetailsById(Guid id, CancellationToken ct);

    /// <summary>
    /// Retrieves all bookings.
    /// </summary>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A read-only list of booking DTOs.</returns>
    Task<IReadOnlyList<GetBookingDto>> GetAllBookings(CancellationToken ct);

    /// <summary>
    /// Retrieves a booking by ID.
    /// </summary>
    /// <param name="id">The booking ID.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>The booking DTO or null if not found.</returns>
    Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken ct);

    /// <summary>
    /// Retrieves all bookings for a specific tour.
    /// </summary>
    /// <param name="tourId">The tour ID.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A read-only list of booking DTOs for the tour.</returns>
    Task<IReadOnlyList<GetBookingDto>> GetBookingsByTourId(Guid tourId, CancellationToken ct);

    /// <summary>
    /// Retrieves all bookings for a specific customer.
    /// </summary>
    /// <param name="customerId">The customer ID.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A read-only list of booking DTOs for the customer.</returns>
    Task<IReadOnlyList<GetBookingDto>> GetBookingsByCustomerId(Guid customerId, CancellationToken ct);
}
