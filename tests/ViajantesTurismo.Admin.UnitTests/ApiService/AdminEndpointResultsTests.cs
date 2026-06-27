using SharedKernel.Results;
using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.Testing.Builders;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

public class AdminEndpointResultsTests
{
    [Theory]
    [MemberData(nameof(NotFoundConflictFailures))]
    public void Match_not_found_conflict_failure_returns_mapped_result(Result result, string expected)
    {
        var response = AdminEndpointResults.MatchNotFoundConflictFailure(
            result,
            () => "not-found",
            () => "conflict");

        Assert.Equal(expected, response);
    }

    [Theory]
    [MemberData(nameof(NotFoundConflictValidationFailures))]
    public void Match_not_found_conflict_validation_failure_returns_mapped_result(Result result, string expected)
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
    public void Match_not_found_validation_failure_returns_mapped_result(Result result, string expected)
    {
        var response = AdminEndpointResults.MatchNotFoundValidationFailure(
            result,
            () => "not-found",
            () => "invalid");

        Assert.Equal(expected, response);
    }

    [Theory]
    [MemberData(nameof(ConflictValidationFailures))]
    public void Match_conflict_validation_failure_returns_mapped_result(Result result, string expected)
    {
        var response = AdminEndpointResults.MatchConflictValidationFailure(
            result,
            () => "conflict",
            () => "invalid");

        Assert.Equal(expected, response);
    }

    [Fact]
    public void Match_not_found_conflict_failure_with_unsupported_status_throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            AdminEndpointResults.MatchNotFoundConflictFailure(
                Result.Ok(),
                () => "not-found",
                () => "conflict"));

        Assert.Equal("Unsupported result status 'Ok'.", exception.Message);
    }

    [Fact]
    public async Task Get_booking_response_when_booking_exists_returns_found_response()
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
    public async Task Get_booking_response_when_booking_is_missing_returns_not_found_response()
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
    public async Task Get_payment_response_when_payment_exists_returns_found_response()
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
    public async Task Get_payment_response_when_booking_is_missing_returns_booking_not_found_response()
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
    public async Task Get_payment_response_when_payment_is_missing_returns_payment_not_found_response()
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
    public async Task Match_not_found_conflict_booking_response_when_result_succeeds_returns_found_response()
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
    public async Task Match_not_found_conflict_validation_booking_response_when_result_is_invalid_returns_invalid_response()
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
    public async Task Match_not_found_validation_booking_response_when_query_misses_booking_returns_not_found_response()
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
