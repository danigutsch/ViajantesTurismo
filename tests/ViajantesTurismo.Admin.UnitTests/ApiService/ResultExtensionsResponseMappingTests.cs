using Microsoft.AspNetCore.Http;
using ViajantesTurismo.Admin.ApiService;
using SharedKernel.Results;
using static ViajantesTurismo.Admin.UnitTests.ApiService.ResultExtensionsResponseMappingTestHelpers;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

public class ResultExtensionsResponseMappingTests
{
    [Fact]
    public void ToValidationProblem_when_result_is_invalid_returns_bad_request_validation_details()
    {
        // Arrange
        var failedResult = Result.Invalid("Validation failed.", "Email", "Email is invalid.");

        // Act
        var validationProblem = failedResult.ToValidationProblem();
        var details = AssertValidationProblemDetails(validationProblem);

        // Assert
        TestAssert.Equal(StatusCodes.Status400BadRequest, validationProblem.StatusCode);
        TestAssert.Equal(StatusCodes.Status400BadRequest, details.Status);
        TestAssert.Equal("Validation failed.", details.Detail);
        TestAssert.Equal(["Email is invalid."], details.Errors["Email"]);
    }

    [Fact]
    public void ToValidationProblem_when_result_is_successful_throws_invalidoperationexception()
    {
        // Arrange
        var successfulResult = Result.Ok();

        // Act
        var exception = TestAssert.Throws<InvalidOperationException>(() => successfulResult.ToValidationProblem());

        // Assert
        TestAssert.Equal("Cannot convert a successful result to a ValidationProblem.", exception.Message);
    }

    [Fact]
    public void ToValidationProblem_when_result_status_is_unknown_throws_invalidoperationexception()
    {
        // Arrange
        var resultWithUnknownStatus = default(Result);

        // Act
        var exception = TestAssert.Throws<InvalidOperationException>(() => resultWithUnknownStatus.ToValidationProblem());

        // Assert
        TestAssert.Equal("Only results with status 'Invalid' can be converted to a ValidationProblem.", exception.Message);
    }

    [Fact]
    public void ToValidationProblem_when_invalid_result_has_no_error_details_throws_invalidoperationexception()
    {
        // Arrange
        var malformedInvalidResult = CreateMalformedFailureResult(ResultStatus.Invalid, null);

        // Act
        var exception = TestAssert.Throws<InvalidOperationException>(() => malformedInvalidResult.ToValidationProblem());

        // Assert
        TestAssert.Equal("Error details are required to convert to a ValidationProblem.", exception.Message);
    }

    [Fact]
    public void ToValidationProblem_when_invalid_result_has_no_validation_errors_throws_invalidoperationexception()
    {
        // Arrange
        var malformedInvalidResult = CreateMalformedFailureResult(ResultStatus.Invalid, new ResultError("Validation failed."));

        // Act
        var exception = TestAssert.Throws<InvalidOperationException>(() => malformedInvalidResult.ToValidationProblem());

        // Assert
        TestAssert.Equal("Validation errors are required to convert to a ValidationProblem.", exception.Message);
    }

    [Fact]
    public void ToValidationProblem_when_generic_result_is_invalid_returns_bad_request_validation_details()
    {
        // Arrange
        var failedResult = Result.Invalid<string>("Validation failed.", "Email", "Email is invalid.");

        // Act
        var validationProblem = failedResult.ToValidationProblem();
        var details = AssertValidationProblemDetails(validationProblem);

        // Assert
        TestAssert.Equal(StatusCodes.Status400BadRequest, validationProblem.StatusCode);
        TestAssert.Equal(StatusCodes.Status400BadRequest, details.Status);
        TestAssert.Equal("Validation failed.", details.Detail);
        TestAssert.Equal(["Email is invalid."], details.Errors["Email"]);
    }

    [Fact]
    public void ToValidationProblem_when_generic_result_is_successful_throws_invalidoperationexception()
    {
        // Arrange
        var successfulResult = Result.Ok("done");

        // Act
        var exception = TestAssert.Throws<InvalidOperationException>(() => successfulResult.ToValidationProblem());

        // Assert
        TestAssert.Equal("Cannot convert a successful result to a ValidationProblem.", exception.Message);
    }

    [Fact]
    public void ToValidationProblem_when_generic_result_status_is_unknown_throws_invalidoperationexception()
    {
        // Arrange
        var resultWithUnknownStatus = default(Result<string>);

        // Act
        var exception = TestAssert.Throws<InvalidOperationException>(() => resultWithUnknownStatus.ToValidationProblem());

        // Assert
        TestAssert.Equal("Only results with status 'Invalid' can be converted to a ValidationProblem.", exception.Message);
    }

    [Fact]
    public void ToValidationProblem_when_generic_invalid_result_has_no_error_details_throws_invalidoperationexception()
    {
        // Arrange
        var malformedInvalidResult = CreateMalformedFailureResult<string>(ResultStatus.Invalid, null, null);

        // Act
        var exception = TestAssert.Throws<InvalidOperationException>(() => malformedInvalidResult.ToValidationProblem());

        // Assert
        TestAssert.Equal("Error details are required to convert to a ValidationProblem.", exception.Message);
    }

    [Fact]
    public void ToValidationProblem_when_generic_invalid_result_has_no_validation_errors_throws_invalidoperationexception()
    {
        // Arrange
        var malformedInvalidResult = CreateMalformedFailureResult<string>(ResultStatus.Invalid, null, new ResultError("Validation failed."));

        // Act
        var exception = TestAssert.Throws<InvalidOperationException>(() => malformedInvalidResult.ToValidationProblem());

        // Assert
        TestAssert.Equal("Validation errors are required to convert to a ValidationProblem.", exception.Message);
    }

    [Fact]
    public void ToNotFound_when_result_is_notfound_returns_not_found_problem_details()
    {
        // Arrange
        var failedResult = Result.NotFound("Customer was not found.");

        // Act
        var notFoundResult = failedResult.ToNotFound();

        // Assert
        TestAssert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        TestAssert.NotNull(notFoundResult.Value);
        TestAssert.Equal("Resource Not Found", notFoundResult.Value.Title);
        TestAssert.Equal("Customer was not found.", notFoundResult.Value.Detail);
        TestAssert.Equal(StatusCodes.Status404NotFound, notFoundResult.Value.Status);
    }

    [Fact]
    public void ToNotFound_when_result_is_successful_throws_invalidoperationexception()
    {
        // Arrange
        var successfulResult = Result.Ok();

        // Act
        var exception = TestAssert.Throws<InvalidOperationException>(() => successfulResult.ToNotFound());

        // Assert
        TestAssert.Equal("Cannot convert a successful result to NotFound.", exception.Message);
    }

    [Fact]
    public void ToNotFound_when_result_status_is_unknown_throws_invalidoperationexception()
    {
        // Arrange
        var resultWithUnknownStatus = default(Result);

        // Act
        var exception = TestAssert.Throws<InvalidOperationException>(() => resultWithUnknownStatus.ToNotFound());

        // Assert
        TestAssert.Equal("Only results with status 'NotFound' can be converted to NotFound.", exception.Message);
    }

    [Fact]
    public void ToNotFound_when_generic_result_is_notfound_returns_not_found_problem_details()
    {
        // Arrange
        var failedResult = Result.NotFound<string>("Customer was not found.");

        // Act
        var notFoundResult = failedResult.ToNotFound();

        // Assert
        TestAssert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        TestAssert.NotNull(notFoundResult.Value);
        TestAssert.Equal("Resource Not Found", notFoundResult.Value.Title);
        TestAssert.Equal("Customer was not found.", notFoundResult.Value.Detail);
        TestAssert.Equal(StatusCodes.Status404NotFound, notFoundResult.Value.Status);
    }

    [Fact]
    public void ToNotFound_when_generic_result_is_successful_throws_invalidoperationexception()
    {
        // Arrange
        var successfulResult = Result.Ok("done");

        // Act
        var exception = TestAssert.Throws<InvalidOperationException>(() => successfulResult.ToNotFound());

        // Assert
        TestAssert.Equal("Cannot convert a successful result to NotFound.", exception.Message);
    }

    [Fact]
    public void ToNotFound_when_generic_result_status_is_unknown_throws_invalidoperationexception()
    {
        // Arrange
        var resultWithUnknownStatus = default(Result<string>);

        // Act
        var exception = TestAssert.Throws<InvalidOperationException>(() => resultWithUnknownStatus.ToNotFound());

        // Assert
        TestAssert.Equal("Only results with status 'NotFound' can be converted to NotFound.", exception.Message);
    }

    [Fact]
    public void ToConflict_when_result_is_conflict_returns_conflict_problem_details()
    {
        // Arrange
        var failedResult = Result.Conflict("Customer already exists.");

        // Act
        var conflictResult = failedResult.ToConflict();

        // Assert
        TestAssert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
        TestAssert.NotNull(conflictResult.Value);
        TestAssert.Equal("Conflict", conflictResult.Value.Title);
        TestAssert.Equal("Customer already exists.", conflictResult.Value.Detail);
        TestAssert.Equal(StatusCodes.Status409Conflict, conflictResult.Value.Status);
    }

    [Fact]
    public void ToConflict_when_result_is_successful_throws_invalidoperationexception()
    {
        // Arrange
        var successfulResult = Result.Ok();

        // Act
        var exception = TestAssert.Throws<InvalidOperationException>(() => successfulResult.ToConflict());

        // Assert
        TestAssert.Equal("Cannot convert a successful result to Conflict.", exception.Message);
    }

    [Fact]
    public void ToConflict_when_result_status_is_unknown_throws_invalidoperationexception()
    {
        // Arrange
        var resultWithUnknownStatus = default(Result);

        // Act
        var exception = TestAssert.Throws<InvalidOperationException>(() => resultWithUnknownStatus.ToConflict());

        // Assert
        TestAssert.Equal("Only results with status 'Conflict' can be converted to Conflict.", exception.Message);
    }

    [Fact]
    public void ToConflict_when_generic_result_is_conflict_returns_conflict_problem_details()
    {
        // Arrange
        var failedResult = Result.Conflict<string>("Customer already exists.");

        // Act
        var conflictResult = failedResult.ToConflict();

        // Assert
        TestAssert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
        TestAssert.NotNull(conflictResult.Value);
        TestAssert.Equal("Conflict", conflictResult.Value.Title);
        TestAssert.Equal("Customer already exists.", conflictResult.Value.Detail);
        TestAssert.Equal(StatusCodes.Status409Conflict, conflictResult.Value.Status);
    }

    [Fact]
    public void ToConflict_when_generic_result_is_successful_throws_invalidoperationexception()
    {
        // Arrange
        var successfulResult = Result.Ok("done");

        // Act
        var exception = TestAssert.Throws<InvalidOperationException>(() => successfulResult.ToConflict());

        // Assert
        TestAssert.Equal("Cannot convert a successful result to Conflict.", exception.Message);
    }

    [Fact]
    public void ToConflict_when_generic_result_status_is_unknown_throws_invalidoperationexception()
    {
        // Arrange
        var resultWithUnknownStatus = default(Result<string>);

        // Act
        var exception = TestAssert.Throws<InvalidOperationException>(() => resultWithUnknownStatus.ToConflict());

        // Assert
        TestAssert.Equal("Only results with status 'Conflict' can be converted to Conflict.", exception.Message);
    }

}
