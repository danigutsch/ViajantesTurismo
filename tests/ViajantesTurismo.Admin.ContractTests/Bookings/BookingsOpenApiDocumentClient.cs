using System.Text.Json;

namespace ViajantesTurismo.Admin.ContractTests.Bookings;

/// <summary>
/// Reads the canonical bookings OpenAPI artifact and maps the consumer-owned slice.
/// </summary>
internal static class BookingsOpenApiDocumentClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Retrieves the subset of the canonical bookings OpenAPI contract that this consumer relies on.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the file read.</param>
    /// <returns>The mapped bookings OpenAPI contract slice.</returns>
    public static async Task<BookingsOpenApiContractDto> GetContract(CancellationToken cancellationToken)
        => await GetContract(GetCanonicalDocumentPath(), cancellationToken);

    /// <summary>
    /// Retrieves the subset of the build-time generated bookings OpenAPI contract used for
    /// generated-vs-canonical compatibility validation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the file read.</param>
    /// <returns>The mapped bookings OpenAPI contract slice from the generated artifact.</returns>
    public static async Task<BookingsOpenApiContractDto> GetGeneratedContract(CancellationToken cancellationToken)
        => await GetContract(GetGeneratedDocumentPath(), cancellationToken);

    private static async Task<BookingsOpenApiContractDto> GetContract(string documentPath, CancellationToken cancellationToken)
    {
        var document = await ReadDocument(documentPath, cancellationToken);

        if (document?.Info is null || document.Paths is null)
        {
            throw new InvalidOperationException("The bookings OpenAPI document is incomplete.");
        }

        if (!document.Paths.TryGetValue("/bookings", out var bookingsPath) ||
            bookingsPath.Get is null ||
            bookingsPath.Post is null)
        {
            throw new InvalidOperationException("The bookings collection path is missing required operations.");
        }

        if (!document.Paths.TryGetValue("/bookings/{id}", out var bookingByIdPath) ||
            bookingByIdPath.Get is null ||
            bookingByIdPath.Delete is null)
        {
            throw new InvalidOperationException("The bookings item path is missing required operations.");
        }

        if (!document.Paths.TryGetValue("/bookings/tour/{tourId}", out var bookingsByTourPath) ||
            bookingsByTourPath.Get is null)
        {
            throw new InvalidOperationException("The bookings-by-tour path is missing the required operation.");
        }

        if (!document.Paths.TryGetValue("/bookings/customer/{customerId}", out var bookingsByCustomerPath) ||
            bookingsByCustomerPath.Get is null)
        {
            throw new InvalidOperationException("The bookings-by-customer path is missing the required operation.");
        }

        if (!document.Paths.TryGetValue("/bookings/{id}/discount", out var discountPath) ||
            discountPath.Put is null)
        {
            throw new InvalidOperationException("The booking discount path is missing the required operation.");
        }

        if (!document.Paths.TryGetValue("/bookings/{id}/details", out var detailsPath) ||
            detailsPath.Put is null)
        {
            throw new InvalidOperationException("The booking details path is missing the required operation.");
        }

        if (!document.Paths.TryGetValue("/bookings/{id}/notes", out var notesPath) ||
            notesPath.Patch is null)
        {
            throw new InvalidOperationException("The booking notes path is missing the required operation.");
        }

        if (!document.Paths.TryGetValue("/bookings/{id}/confirm", out var confirmPath) ||
            confirmPath.Post is null)
        {
            throw new InvalidOperationException("The booking confirm path is missing the required operation.");
        }

        if (!document.Paths.TryGetValue("/bookings/{id}/cancel", out var cancelPath) ||
            cancelPath.Post is null)
        {
            throw new InvalidOperationException("The booking cancel path is missing the required operation.");
        }

        if (!document.Paths.TryGetValue("/bookings/{id}/complete", out var completePath) ||
            completePath.Post is null)
        {
            throw new InvalidOperationException("The booking complete path is missing the required operation.");
        }

        if (!document.Paths.TryGetValue("/bookings/{id}/payments", out var paymentsPath) ||
            paymentsPath.Post is null)
        {
            throw new InvalidOperationException("The booking payments path is missing the required operation.");
        }

        var createBookingSchemaReference = bookingsPath.Post.RequestBody?.Content?.ApplicationJson?.Schema?.Reference;
        var updateBookingDiscountSchemaReference = discountPath.Put.RequestBody?.Content?.ApplicationJson?.Schema?.Reference;
        var updateBookingDetailsSchemaReference = detailsPath.Put.RequestBody?.Content?.ApplicationJson?.Schema?.Reference;
        var updateBookingNotesSchemaReference = notesPath.Patch.RequestBody?.Content?.ApplicationJson?.Schema?.Reference;
        var recordPaymentSchemaReference = paymentsPath.Post.RequestBody?.Content?.ApplicationJson?.Schema?.Reference;

        if (string.IsNullOrWhiteSpace(createBookingSchemaReference) ||
            string.IsNullOrWhiteSpace(updateBookingDiscountSchemaReference) ||
            string.IsNullOrWhiteSpace(updateBookingDetailsSchemaReference) ||
            string.IsNullOrWhiteSpace(updateBookingNotesSchemaReference) ||
            string.IsNullOrWhiteSpace(recordPaymentSchemaReference))
        {
            throw new InvalidOperationException("The bookings contract is missing request schema references.");
        }

        return new BookingsOpenApiContractDto(
            document.OpenApi,
            document.Info.Title,
            bookingsPath.Get.OperationId,
            bookingByIdPath.Get.OperationId,
            bookingsPath.Post.OperationId,
            createBookingSchemaReference,
            bookingByIdPath.Delete.OperationId,
            bookingsByTourPath.Get.OperationId,
            bookingsByCustomerPath.Get.OperationId,
            discountPath.Put.OperationId,
            updateBookingDiscountSchemaReference,
            detailsPath.Put.OperationId,
            updateBookingDetailsSchemaReference,
            notesPath.Patch.OperationId,
            updateBookingNotesSchemaReference,
            confirmPath.Post.OperationId,
            cancelPath.Post.OperationId,
            completePath.Post.OperationId,
            paymentsPath.Post.OperationId,
            recordPaymentSchemaReference);
    }

    private static async Task<BookingsOpenApiDocumentDto?> ReadDocument(string documentPath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(documentPath);
        return await JsonSerializer.DeserializeAsync<BookingsOpenApiDocumentDto>(stream, SerializerOptions, cancellationToken);
    }

    private static string GetCanonicalDocumentPath()
        => Path.Combine(GetRepositoryRoot(), "src", "ViajantesTurismo.Admin.Contracts", "OpenApi", "bookings.openapi.json");

    private static string GetGeneratedDocumentPath()
        => Path.Combine(GetRepositoryRoot(), "src", "ViajantesTurismo.Admin.Contracts", "OpenApi", ".generated", "ViajantesTurismo.Admin.ApiService_bookings.json");

    private static string GetRepositoryRoot()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            var candidatePath = Path.Combine(currentDirectory.FullName, "ViajantesTurismo.slnx");
            if (File.Exists(candidatePath))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root for contract test artifacts.");
    }
}
