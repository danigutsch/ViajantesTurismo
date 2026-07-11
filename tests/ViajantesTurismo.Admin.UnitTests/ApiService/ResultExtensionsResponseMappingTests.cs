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
        (validationProblem.StatusCode).ShouldBe(StatusCodes.Status400BadRequest);
        (details.Status).ShouldBe(StatusCodes.Status400BadRequest);
        (details.Detail).ShouldBe("Validation failed.");
        (details.Errors["Email"]).ShouldBe(["Email is invalid."]);
    }

    [Fact]
    public void ToValidationProblem_when_result_is_successful_throws_invalidoperationexception()
    {
        // Arrange
        var successfulResult = Result.Ok();

        // Act
        var exception = ((Func<object?>)(() => successfulResult.ToValidationProblem())).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("Cannot convert a successful result to a ValidationProblem.");
    }

    [Fact]
    public void ToValidationProblem_when_result_status_is_unknown_throws_invalidoperationexception()
    {
        // Arrange
        var resultWithUnknownStatus = default(Result);

        // Act
        var exception = ((Func<object?>)(() => resultWithUnknownStatus.ToValidationProblem())).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("Only results with status 'Invalid' can be converted to a ValidationProblem.");
    }

    [Fact]
    public void ToValidationProblem_when_invalid_result_has_no_error_details_throws_invalidoperationexception()
    {
        // Arrange
        var malformedInvalidResult = CreateMalformedFailureResult(ResultStatus.Invalid, null);

        // Act
        var exception = ((Func<object?>)(() => malformedInvalidResult.ToValidationProblem())).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("Error details are required to convert to a ValidationProblem.");
    }

    [Fact]
    public void ToValidationProblem_when_invalid_result_has_no_validation_errors_throws_invalidoperationexception()
    {
        // Arrange
        var malformedInvalidResult = CreateMalformedFailureResult(ResultStatus.Invalid, new ResultError("Validation failed."));

        // Act
        var exception = ((Func<object?>)(() => malformedInvalidResult.ToValidationProblem())).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("Validation errors are required to convert to a ValidationProblem.");
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
        (validationProblem.StatusCode).ShouldBe(StatusCodes.Status400BadRequest);
        (details.Status).ShouldBe(StatusCodes.Status400BadRequest);
        (details.Detail).ShouldBe("Validation failed.");
        (details.Errors["Email"]).ShouldBe(["Email is invalid."]);
    }

    [Fact]
    public void ToValidationProblem_when_generic_result_is_successful_throws_invalidoperationexception()
    {
        // Arrange
        var successfulResult = Result.Ok("done");

        // Act
        var exception = ((Func<object?>)(() => successfulResult.ToValidationProblem())).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("Cannot convert a successful result to a ValidationProblem.");
    }

    [Fact]
    public void ToValidationProblem_when_generic_result_status_is_unknown_throws_invalidoperationexception()
    {
        // Arrange
        var resultWithUnknownStatus = default(Result<string>);

        // Act
        var exception = ((Func<object?>)(() => resultWithUnknownStatus.ToValidationProblem())).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("Only results with status 'Invalid' can be converted to a ValidationProblem.");
    }

    [Fact]
    public void ToValidationProblem_when_generic_invalid_result_has_no_error_details_throws_invalidoperationexception()
    {
        // Arrange
        var malformedInvalidResult = CreateMalformedFailureResult<string>(ResultStatus.Invalid, null, null);

        // Act
        var exception = ((Func<object?>)(() => malformedInvalidResult.ToValidationProblem())).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("Error details are required to convert to a ValidationProblem.");
    }

    [Fact]
    public void ToValidationProblem_when_generic_invalid_result_has_no_validation_errors_throws_invalidoperationexception()
    {
        // Arrange
        var malformedInvalidResult = CreateMalformedFailureResult<string>(ResultStatus.Invalid, null, new ResultError("Validation failed."));

        // Act
        var exception = ((Func<object?>)(() => malformedInvalidResult.ToValidationProblem())).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("Validation errors are required to convert to a ValidationProblem.");
    }

    [Fact]
    public void ToNotFound_when_result_is_notfound_returns_not_found_problem_details()
    {
        // Arrange
        var failedResult = Result.NotFound("Customer was not found.");

        // Act
        var notFoundResult = failedResult.ToNotFound();

        // Assert
        (notFoundResult.StatusCode).ShouldBe(StatusCodes.Status404NotFound);
        (notFoundResult.Value).ShouldNotBeNull();
        (notFoundResult.Value.Title).ShouldBe("Resource Not Found");
        (notFoundResult.Value.Detail).ShouldBe("Customer was not found.");
        (notFoundResult.Value.Status).ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void ToNotFound_when_result_is_successful_throws_invalidoperationexception()
    {
        // Arrange
        var successfulResult = Result.Ok();

        // Act
        var exception = ((Func<object?>)(() => successfulResult.ToNotFound())).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("Cannot convert a successful result to NotFound.");
    }

    [Fact]
    public void ToNotFound_when_result_status_is_unknown_throws_invalidoperationexception()
    {
        // Arrange
        var resultWithUnknownStatus = default(Result);

        // Act
        var exception = ((Func<object?>)(() => resultWithUnknownStatus.ToNotFound())).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("Only results with status 'NotFound' can be converted to NotFound.");
    }

    [Fact]
    public void ToNotFound_when_generic_result_is_notfound_returns_not_found_problem_details()
    {
        // Arrange
        var failedResult = Result.NotFound<string>("Customer was not found.");

        // Act
        var notFoundResult = failedResult.ToNotFound();

        // Assert
        (notFoundResult.StatusCode).ShouldBe(StatusCodes.Status404NotFound);
        (notFoundResult.Value).ShouldNotBeNull();
        (notFoundResult.Value.Title).ShouldBe("Resource Not Found");
        (notFoundResult.Value.Detail).ShouldBe("Customer was not found.");
        (notFoundResult.Value.Status).ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void ToNotFound_when_generic_result_is_successful_throws_invalidoperationexception()
    {
        // Arrange
        var successfulResult = Result.Ok("done");

        // Act
        var exception = ((Func<object?>)(() => successfulResult.ToNotFound())).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("Cannot convert a successful result to NotFound.");
    }

    [Fact]
    public void ToNotFound_when_generic_result_status_is_unknown_throws_invalidoperationexception()
    {
        // Arrange
        var resultWithUnknownStatus = default(Result<string>);

        // Act
        var exception = ((Func<object?>)(() => resultWithUnknownStatus.ToNotFound())).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("Only results with status 'NotFound' can be converted to NotFound.");
    }

    [Fact]
    public void ToConflict_when_result_is_conflict_returns_conflict_problem_details()
    {
        // Arrange
        var failedResult = Result.Conflict("Customer already exists.");

        // Act
        var conflictResult = failedResult.ToConflict();

        // Assert
        (conflictResult.StatusCode).ShouldBe(StatusCodes.Status409Conflict);
        (conflictResult.Value).ShouldNotBeNull();
        (conflictResult.Value.Title).ShouldBe("Conflict");
        (conflictResult.Value.Detail).ShouldBe("Customer already exists.");
        (conflictResult.Value.Status).ShouldBe(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void ToConflict_when_result_is_successful_throws_invalidoperationexception()
    {
        // Arrange
        var successfulResult = Result.Ok();

        // Act
        var exception = ((Func<object?>)(() => successfulResult.ToConflict())).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("Cannot convert a successful result to Conflict.");
    }

    [Fact]
    public void ToConflict_when_result_status_is_unknown_throws_invalidoperationexception()
    {
        // Arrange
        var resultWithUnknownStatus = default(Result);

        // Act
        var exception = ((Func<object?>)(() => resultWithUnknownStatus.ToConflict())).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("Only results with status 'Conflict' can be converted to Conflict.");
    }

    [Fact]
    public void ToConflict_when_generic_result_is_conflict_returns_conflict_problem_details()
    {
        // Arrange
        var failedResult = Result.Conflict<string>("Customer already exists.");

        // Act
        var conflictResult = failedResult.ToConflict();

        // Assert
        (conflictResult.StatusCode).ShouldBe(StatusCodes.Status409Conflict);
        (conflictResult.Value).ShouldNotBeNull();
        (conflictResult.Value.Title).ShouldBe("Conflict");
        (conflictResult.Value.Detail).ShouldBe("Customer already exists.");
        (conflictResult.Value.Status).ShouldBe(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void ToConflict_when_generic_result_is_successful_throws_invalidoperationexception()
    {
        // Arrange
        var successfulResult = Result.Ok("done");

        // Act
        var exception = ((Func<object?>)(() => successfulResult.ToConflict())).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("Cannot convert a successful result to Conflict.");
    }

    [Fact]
    public void ToConflict_when_generic_result_status_is_unknown_throws_invalidoperationexception()
    {
        // Arrange
        var resultWithUnknownStatus = default(Result<string>);

        // Act
        var exception = ((Func<object?>)(() => resultWithUnknownStatus.ToConflict())).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("Only results with status 'Conflict' can be converted to Conflict.");
    }

}
