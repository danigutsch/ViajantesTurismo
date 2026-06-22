using SharedKernel.AspNet;

namespace ViajantesTurismo.Admin.ApiService;

internal static class AdminEndpoints
{
    internal static class Tours
    {
        public static EndpointDefinition Create { get; } = new(
            "/",
            new EndpointMetadata("CreateTour", "Creates a new tour.", "Creates a new tour."));

        public static EndpointDefinition GetAll { get; } = new(
            "/",
            new EndpointMetadata("GetTours", "Retrieves all available tours.", "Retrieves all available tours."));

        public static EndpointDefinition GetById { get; } = new(
            "/{id:guid}",
            new EndpointMetadata("GetTourById", "Retrieves a tour by its ID.", "Retrieves a tour by its ID."));

        public static EndpointDefinition Update { get; } = new(
            "/{id:guid}",
            new EndpointMetadata("UpdateTour", "Updates an existing tour.", "Updates an existing tour."));
    }

    internal static class Customers
    {
        public static EndpointDefinition GetAll { get; } = new(
            "/",
            new EndpointMetadata("GetCustomers", "Retrieves all customers.", "Retrieves all customers."));

        public static EndpointDefinition GetById { get; } = new(
            "/{id:guid}",
            new EndpointMetadata("GetCustomerById", "Retrieves a customer by their ID.", "Retrieves a customer by their ID."));

        public static EndpointDefinition Create { get; } = new(
            "/",
            new EndpointMetadata("CreateCustomer", "Creates a new customer.", "Creates a new customer with all required information."));

        public static EndpointDefinition Update { get; } = new(
            "/{id:guid}",
            new EndpointMetadata("UpdateCustomer", "Updates an existing customer.", "Updates an existing customer."));
    }

    internal static class CustomerImports
    {
        public static EndpointDefinition Import { get; } = new(
            "/",
            new EndpointMetadata("ImportCustomers", "Imports customers from a CSV file.", "Imports customers from a CSV file."));

        public static EndpointDefinition Commit { get; } = new(
            "/commit",
            new EndpointMetadata(
                "CommitImportWithResolutions",
                "Commits customer import applying conflict resolutions.",
                "Commits customer import applying conflict resolutions."));
    }

    internal static class Bookings
    {
        public static EndpointDefinition GetAll { get; } = new(
            "/",
            new EndpointMetadata("GetBookings", "Retrieves all bookings.", "Retrieves all bookings."));

        public static EndpointDefinition GetById { get; } = new(
            "/{id:guid}",
            new EndpointMetadata("GetBookingById", "Retrieves a booking by its ID.", "Retrieves a booking by its ID."));

        public static EndpointDefinition GetByTourId { get; } = new(
            "/tour/{tourId:guid}",
            new EndpointMetadata(
                "GetBookingsByTourId",
                "Retrieves all bookings for a specific tour.",
                "Retrieves all bookings for a specific tour."));

        public static EndpointDefinition GetByCustomerId { get; } = new(
            "/customer/{customerId:guid}",
            new EndpointMetadata(
                "GetBookingsByCustomerId",
                "Retrieves all bookings for a specific customer.",
                "Retrieves all bookings for a specific customer (as primary or companion)."));

        public static EndpointDefinition Create { get; } = new(
            "/",
            new EndpointMetadata("CreateBooking", "Creates a new booking.", "Creates a new booking for a tour."));

        public static EndpointDefinition UpdateDiscount { get; } = new(
            "/{id:guid}/discount",
            new EndpointMetadata("UpdateBookingDiscount", "Updates booking discount.", "Updates the discount for a booking."));

        public static EndpointDefinition UpdateDetails { get; } = new(
            "/{id:guid}/details",
            new EndpointMetadata(
                "UpdateBookingDetails",
                "Updates booking details.",
                "Updates booking details (room type, bikes, companion)."));

        public static EndpointDefinition Delete { get; } = new(
            "/{id:guid}",
            new EndpointMetadata("DeleteBooking", "Deletes a booking.", "Deletes a booking."));

        public static EndpointDefinition Cancel { get; } = new(
            "/{id:guid}/cancel",
            new EndpointMetadata(
                "CancelBooking",
                "Cancels a booking.",
                "Cancels a booking by transitioning its status to Cancelled."));

        public static EndpointDefinition Confirm { get; } = new(
            "/{id:guid}/confirm",
            new EndpointMetadata(
                "ConfirmBooking",
                "Confirms a booking.",
                "Confirms a booking by transitioning its status to Confirmed."));

        public static EndpointDefinition UpdateNotes { get; } = new(
            "/{id:guid}/notes",
            new EndpointMetadata("UpdateBookingNotes", "Updates booking notes.", "Updates the notes of a booking."));

        public static EndpointDefinition Complete { get; } = new(
            "/{id:guid}/complete",
            new EndpointMetadata(
                "CompleteBooking",
                "Completes a booking.",
                "Completes a booking by transitioning its status to Completed."));

        public static EndpointDefinition RecordPayment { get; } = new(
            "/{id:guid}/payments",
            new EndpointMetadata("RecordPayment", "Records a payment.", "Records a payment for a booking."));
    }

    internal static class ErrorDocumentation
    {
        public static EndpointDefinition GetAll { get; } = new(
            "/",
            new EndpointMetadata(
                "GetErrorDocumentation",
                "Retrieves generated error documentation metadata.",
                "Retrieves generated documentation metadata for centralized error providers."));

        public static EndpointDefinition GetByIdentifier { get; } = new(
            "/{identifier}",
            new EndpointMetadata(
                "GetErrorDocumentationByIdentifier",
                "Retrieves one generated error documentation entry.",
                "Retrieves one generated centralized error documentation entry by identifier."));
    }
}
