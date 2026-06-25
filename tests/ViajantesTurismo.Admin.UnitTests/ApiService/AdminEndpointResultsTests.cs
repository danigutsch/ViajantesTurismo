using SharedKernel.Results;
using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Testing.Builders;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

public class AdminEndpointResultsTests
{
    [Theory]
    [MemberData(nameof(NotFoundConflictFailures))]
    public void Match_Not_Found_Conflict_Failure_Returns_Mapped_Result(Result result, string expected)
    {
        var response = AdminEndpointResults.MatchNotFoundConflictFailure(
            result,
            () => "not-found",
            () => "conflict");

        Assert.Equal(expected, response);
    }

    [Theory]
    [MemberData(nameof(NotFoundConflictValidationFailures))]
    public void Match_Not_Found_Conflict_Validation_Failure_Returns_Mapped_Result(Result result, string expected)
    {
        var response = AdminEndpointResults.MatchNotFoundConflictValidationFailure(
            result,
            () => "not-found",
            () => "conflict",
            () => "invalid");

        Assert.Equal(expected, response);
    }

    [Theory]
    [MemberData(nameof(NotFoundValidationFailures))]
    public void Match_Not_Found_Validation_Failure_Returns_Mapped_Result(Result result, string expected)
    {
        var response = AdminEndpointResults.MatchNotFoundValidationFailure(
            result,
            () => "not-found",
            () => "invalid");

        Assert.Equal(expected, response);
    }

    [Theory]
    [MemberData(nameof(ConflictValidationFailures))]
    public void Match_Conflict_Validation_Failure_Returns_Mapped_Result(Result result, string expected)
    {
        var response = AdminEndpointResults.MatchConflictValidationFailure(
            result,
            () => "conflict",
            () => "invalid");

        Assert.Equal(expected, response);
    }

    [Fact]
    public void Match_Not_Found_Conflict_Failure_With_Unsupported_Status_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            AdminEndpointResults.MatchNotFoundConflictFailure(
                Result.Ok(),
                () => "not-found",
                () => "conflict"));

        Assert.Equal("Unsupported result status 'Ok'.", exception.Message);
    }

    [Fact]
    public async Task Get_Booking_Response_When_Booking_Exists_Returns_Found_Response()
    {
        var booking = DtoBuilders.BuildBookingDto();
        var queryService = new FakeQueryService(booking, null);

        var response = await AdminEndpointResults.GetBookingResponse(
            booking.Id,
            queryService,
            foundBooking => foundBooking.Id.ToString(),
            _ => "missing",
            CancellationToken.None);

        Assert.Equal(booking.Id.ToString(), response);
    }

    [Fact]
    public async Task Get_Booking_Response_When_Booking_Is_Missing_Returns_Not_Found_Response()
    {
        var bookingId = Guid.CreateVersion7();
        var queryService = new FakeQueryService(null, null);

        var response = await AdminEndpointResults.GetBookingResponse(
            bookingId,
            queryService,
            foundBooking => foundBooking.Id.ToString(),
            missingBookingId => missingBookingId.ToString(),
            CancellationToken.None);

        Assert.Equal(bookingId.ToString(), response);
    }

    [Fact]
    public async Task Get_Payment_Response_When_Payment_Exists_Returns_Found_Response()
    {
        var payment = DtoBuilders.BuildPaymentDto();
        var booking = DtoBuilders.BuildBookingDto(id: payment.BookingId, payments: [payment]);
        var queryService = new FakeQueryService(booking, null);

        var response = await AdminEndpointResults.GetPaymentResponse(
            booking.Id,
            payment.Id,
            queryService,
            foundPayment => foundPayment.Id.ToString(),
            _ => "booking",
            _ => "payment",
            CancellationToken.None);

        Assert.Equal(payment.Id.ToString(), response);
    }

    [Fact]
    public async Task Get_Payment_Response_When_Booking_Is_Missing_Returns_Booking_Not_Found_Response()
    {
        var bookingId = Guid.CreateVersion7();
        var paymentId = Guid.CreateVersion7();
        var queryService = new FakeQueryService(null, null);

        var response = await AdminEndpointResults.GetPaymentResponse(
            bookingId,
            paymentId,
            queryService,
            foundPayment => foundPayment.Id.ToString(),
            missingBookingId => missingBookingId.ToString(),
            missingPaymentId => missingPaymentId.ToString(),
            CancellationToken.None);

        Assert.Equal(bookingId.ToString(), response);
    }

    [Fact]
    public async Task Get_Payment_Response_When_Payment_Is_Missing_Returns_Payment_Not_Found_Response()
    {
        var bookingId = Guid.CreateVersion7();
        var paymentId = Guid.CreateVersion7();
        var booking = DtoBuilders.BuildBookingDto(id: bookingId, payments: []);
        var queryService = new FakeQueryService(booking, null);

        var response = await AdminEndpointResults.GetPaymentResponse(
            bookingId,
            paymentId,
            queryService,
            foundPayment => foundPayment.Id.ToString(),
            missingBookingId => missingBookingId.ToString(),
            missingPaymentId => missingPaymentId.ToString(),
            CancellationToken.None);

        Assert.Equal(paymentId.ToString(), response);
    }

    [Fact]
    public async Task Match_Not_Found_Conflict_Booking_Response_When_Result_Succeeds_Returns_Found_Response()
    {
        var booking = DtoBuilders.BuildBookingDto();
        var queryService = new FakeQueryService(booking, null);

        var response = await AdminEndpointResults.MatchNotFoundConflictBookingResponse(
            Result.Ok(),
            booking.Id,
            queryService,
            foundBooking => foundBooking.Id.ToString(),
            () => "not-found",
            () => "conflict",
            CancellationToken.None);

        Assert.Equal(booking.Id.ToString(), response);
    }

    [Fact]
    public async Task Match_Not_Found_Conflict_Validation_Booking_Response_When_Result_Is_Invalid_Returns_Invalid_Response()
    {
        var booking = DtoBuilders.BuildBookingDto();
        var queryService = new FakeQueryService(booking, null);

        var response = await AdminEndpointResults.MatchNotFoundConflictValidationBookingResponse(
            Result.Invalid("invalid", "field", "error"),
            booking.Id,
            queryService,
            foundBooking => foundBooking.Id.ToString(),
            () => "not-found",
            () => "conflict",
            () => "invalid",
            CancellationToken.None);

        Assert.Equal("invalid", response);
    }

    [Fact]
    public async Task Match_Not_Found_Validation_Booking_Response_When_Query_Misses_Booking_Returns_Not_Found_Response()
    {
        var bookingId = Guid.CreateVersion7();
        var queryService = new FakeQueryService(null, null);

        var response = await AdminEndpointResults.MatchNotFoundValidationBookingResponse(
            Result.Ok(),
            bookingId,
            queryService,
            foundBooking => foundBooking.Id.ToString(),
            () => "not-found",
            () => "invalid",
            CancellationToken.None);

        Assert.Equal("not-found", response);
    }

    public static TheoryData<Result, string> NotFoundConflictFailures =>
        new()
        {
            { Result.NotFound("missing"), "not-found" },
            { Result.Conflict("conflict"), "conflict" },
        };

    public static TheoryData<Result, string> NotFoundConflictValidationFailures =>
        new()
        {
            { Result.NotFound("missing"), "not-found" },
            { Result.Conflict("conflict"), "conflict" },
            { Result.Invalid("invalid", "field", "error"), "invalid" },
        };

    public static TheoryData<Result, string> NotFoundValidationFailures =>
        new()
        {
            { Result.NotFound("missing"), "not-found" },
            { Result.Invalid("invalid", "field", "error"), "invalid" },
        };

    public static TheoryData<Result, string> ConflictValidationFailures =>
        new()
        {
            { Result.Conflict("conflict"), "conflict" },
            { Result.Invalid("invalid", "field", "error"), "invalid" },
        };

}

file sealed class FakeQueryService(GetBookingDto? booking, IReadOnlyList<GetBookingDto>? bookings = null) : IQueryService
{
    public Task<IReadOnlyList<GetTourDto>> GetAllTours(CancellationToken ct) => throw new NotSupportedException();

    public Task<GetTourDto?> GetTourById(Guid id, CancellationToken ct) => throw new NotSupportedException();

    public Task<IReadOnlyList<GetCustomerDto>> GetAllCustomers(CancellationToken ct) => throw new NotSupportedException();

    public Task<CustomerDetailsDto?> GetCustomerDetailsById(Guid id, CancellationToken ct) => throw new NotSupportedException();

    public Task<IReadOnlyList<GetBookingDto>> GetAllBookings(CancellationToken ct) =>
        Task.FromResult(bookings ?? (IReadOnlyList<GetBookingDto>)[]);

    public Task<GetBookingDto?> GetBookingById(Guid id, CancellationToken ct) =>
        Task.FromResult(booking?.Id == id ? booking : null);

    public Task<IReadOnlyList<GetBookingDto>> GetBookingsByTourId(Guid tourId, CancellationToken ct) =>
        Task.FromResult(bookings ?? (IReadOnlyList<GetBookingDto>)[]);

    public Task<IReadOnlyList<GetBookingDto>> GetBookingsByCustomerId(Guid customerId, CancellationToken ct) =>
        Task.FromResult(bookings ?? (IReadOnlyList<GetBookingDto>)[]);
}
