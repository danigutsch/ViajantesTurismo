using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

public class ResultExtensionsResponseMappingTests
{
    [Fact]
    public void ToValidationProblem_When_Result_Is_Invalid_Returns_Bad_Request_Validation_Details()
    {
        // Arrange
        var failedResult = Result.Invalid("Validation failed.", "Email", "Email is invalid.");

        // Act
        var validationProblem = failedResult.ToValidationProblem();
        var details = AssertValidationProblemDetails(validationProblem);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, validationProblem.StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, details.Status);
        Assert.Equal("Validation failed.", details.Detail);
        Assert.Equal(["Email is invalid."], details.Errors["Email"]);
    }

    [Fact]
    public void ToValidationProblem_When_Result_Is_Successful_Throws_InvalidOperationException()
    {
        // Arrange
        var successfulResult = Result.Ok();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => successfulResult.ToValidationProblem());

        // Assert
        Assert.Equal("Cannot convert a successful result to a ValidationProblem.", exception.Message);
    }

    [Fact]
    public void ToValidationProblem_When_Result_Status_Is_Unknown_Throws_InvalidOperationException()
    {
        // Arrange
        var resultWithUnknownStatus = default(Result);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => resultWithUnknownStatus.ToValidationProblem());

        // Assert
        Assert.Equal("Only results with status 'Invalid' can be converted to a ValidationProblem.", exception.Message);
    }

    [Fact]
    public void ToValidationProblem_When_Invalid_Result_Has_No_Error_Details_Throws_InvalidOperationException()
    {
        // Arrange
        var malformedInvalidResult = CreateMalformedFailureResult(ResultStatus.Invalid, null);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => malformedInvalidResult.ToValidationProblem());

        // Assert
        Assert.Equal("Error details are required to convert to a ValidationProblem.", exception.Message);
    }

    [Fact]
    public void ToValidationProblem_When_Invalid_Result_Has_No_Validation_Errors_Throws_InvalidOperationException()
    {
        // Arrange
        var malformedInvalidResult = CreateMalformedFailureResult(ResultStatus.Invalid, new ResultError("Validation failed."));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => malformedInvalidResult.ToValidationProblem());

        // Assert
        Assert.Equal("Validation errors are required to convert to a ValidationProblem.", exception.Message);
    }

    [Fact]
    public void ToValidationProblem_When_Generic_Result_Is_Invalid_Returns_Bad_Request_Validation_Details()
    {
        // Arrange
        var failedResult = Result<string>.Invalid("Validation failed.", "Email", "Email is invalid.");

        // Act
        var validationProblem = failedResult.ToValidationProblem();
        var details = AssertValidationProblemDetails(validationProblem);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, validationProblem.StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, details.Status);
        Assert.Equal("Validation failed.", details.Detail);
        Assert.Equal(["Email is invalid."], details.Errors["Email"]);
    }

    [Fact]
    public void ToValidationProblem_When_Generic_Result_Is_Successful_Throws_InvalidOperationException()
    {
        // Arrange
        var successfulResult = Result<string>.Ok("done");

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => successfulResult.ToValidationProblem());

        // Assert
        Assert.Equal("Cannot convert a successful result to a ValidationProblem.", exception.Message);
    }

    [Fact]
    public void ToValidationProblem_When_Generic_Result_Status_Is_Unknown_Throws_InvalidOperationException()
    {
        // Arrange
        var resultWithUnknownStatus = default(Result<string>);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => resultWithUnknownStatus.ToValidationProblem());

        // Assert
        Assert.Equal("Only results with status 'Invalid' can be converted to a ValidationProblem.", exception.Message);
    }

    [Fact]
    public void ToValidationProblem_When_Generic_Invalid_Result_Has_No_Error_Details_Throws_InvalidOperationException()
    {
        // Arrange
        var malformedInvalidResult = CreateMalformedFailureResult<string>(ResultStatus.Invalid, null, null);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => malformedInvalidResult.ToValidationProblem());

        // Assert
        Assert.Equal("Error details are required to convert to a ValidationProblem.", exception.Message);
    }

    [Fact]
    public void ToValidationProblem_When_Generic_Invalid_Result_Has_No_Validation_Errors_Throws_InvalidOperationException()
    {
        // Arrange
        var malformedInvalidResult = CreateMalformedFailureResult<string>(ResultStatus.Invalid, null, new ResultError("Validation failed."));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => malformedInvalidResult.ToValidationProblem());

        // Assert
        Assert.Equal("Validation errors are required to convert to a ValidationProblem.", exception.Message);
    }

    [Fact]
    public void ToNotFound_When_Result_Is_NotFound_Returns_Not_Found_Problem_Details()
    {
        // Arrange
        var failedResult = Result.NotFound("Customer was not found.");

        // Act
        var notFoundResult = failedResult.ToNotFound();

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        Assert.NotNull(notFoundResult.Value);
        Assert.Equal("Resource Not Found", notFoundResult.Value.Title);
        Assert.Equal("Customer was not found.", notFoundResult.Value.Detail);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.Value.Status);
    }

    [Fact]
    public void ToNotFound_When_Result_Is_Successful_Throws_InvalidOperationException()
    {
        // Arrange
        var successfulResult = Result.Ok();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => successfulResult.ToNotFound());

        // Assert
        Assert.Equal("Cannot convert a successful result to NotFound.", exception.Message);
    }

    [Fact]
    public void ToNotFound_When_Result_Status_Is_Unknown_Throws_InvalidOperationException()
    {
        // Arrange
        var resultWithUnknownStatus = default(Result);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => resultWithUnknownStatus.ToNotFound());

        // Assert
        Assert.Equal("Only results with status 'NotFound' can be converted to NotFound.", exception.Message);
    }

    [Fact]
    public void ToNotFound_When_Generic_Result_Is_NotFound_Returns_Not_Found_Problem_Details()
    {
        // Arrange
        var failedResult = Result<string>.NotFound("Customer was not found.");

        // Act
        var notFoundResult = failedResult.ToNotFound();

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        Assert.NotNull(notFoundResult.Value);
        Assert.Equal("Resource Not Found", notFoundResult.Value.Title);
        Assert.Equal("Customer was not found.", notFoundResult.Value.Detail);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.Value.Status);
    }

    [Fact]
    public void ToNotFound_When_Generic_Result_Is_Successful_Throws_InvalidOperationException()
    {
        // Arrange
        var successfulResult = Result<string>.Ok("done");

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => successfulResult.ToNotFound());

        // Assert
        Assert.Equal("Cannot convert a successful result to NotFound.", exception.Message);
    }

    [Fact]
    public void ToNotFound_When_Generic_Result_Status_Is_Unknown_Throws_InvalidOperationException()
    {
        // Arrange
        var resultWithUnknownStatus = default(Result<string>);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => resultWithUnknownStatus.ToNotFound());

        // Assert
        Assert.Equal("Only results with status 'NotFound' can be converted to NotFound.", exception.Message);
    }

    [Fact]
    public void ToConflict_When_Result_Is_Conflict_Returns_Conflict_Problem_Details()
    {
        // Arrange
        var failedResult = Result.Conflict("Customer already exists.");

        // Act
        var conflictResult = failedResult.ToConflict();

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
        Assert.NotNull(conflictResult.Value);
        Assert.Equal("Conflict", conflictResult.Value.Title);
        Assert.Equal("Customer already exists.", conflictResult.Value.Detail);
        Assert.Equal(StatusCodes.Status409Conflict, conflictResult.Value.Status);
    }

    [Fact]
    public void ToConflict_When_Result_Is_Successful_Throws_InvalidOperationException()
    {
        // Arrange
        var successfulResult = Result.Ok();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => successfulResult.ToConflict());

        // Assert
        Assert.Equal("Cannot convert a successful result to Conflict.", exception.Message);
    }

    [Fact]
    public void ToConflict_When_Result_Status_Is_Unknown_Throws_InvalidOperationException()
    {
        // Arrange
        var resultWithUnknownStatus = default(Result);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => resultWithUnknownStatus.ToConflict());

        // Assert
        Assert.Equal("Only results with status 'Conflict' can be converted to Conflict.", exception.Message);
    }

    [Fact]
    public void ToConflict_When_Generic_Result_Is_Conflict_Returns_Conflict_Problem_Details()
    {
        // Arrange
        var failedResult = Result<string>.Conflict("Customer already exists.");

        // Act
        var conflictResult = failedResult.ToConflict();

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
        Assert.NotNull(conflictResult.Value);
        Assert.Equal("Conflict", conflictResult.Value.Title);
        Assert.Equal("Customer already exists.", conflictResult.Value.Detail);
        Assert.Equal(StatusCodes.Status409Conflict, conflictResult.Value.Status);
    }

    [Fact]
    public void ToConflict_When_Generic_Result_Is_Successful_Throws_InvalidOperationException()
    {
        // Arrange
        var successfulResult = Result<string>.Ok("done");

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => successfulResult.ToConflict());

        // Assert
        Assert.Equal("Cannot convert a successful result to Conflict.", exception.Message);
    }

    [Fact]
    public void ToConflict_When_Generic_Result_Status_Is_Unknown_Throws_InvalidOperationException()
    {
        // Arrange
        var resultWithUnknownStatus = default(Result<string>);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => resultWithUnknownStatus.ToConflict());

        // Assert
        Assert.Equal("Only results with status 'Conflict' can be converted to Conflict.", exception.Message);
    }

    private static HttpValidationProblemDetails AssertValidationProblemDetails(ValidationProblem result)
    {
        var valueResult = Assert.IsAssignableFrom<IValueHttpResult<HttpValidationProblemDetails>>(result);
        return Assert.IsType<HttpValidationProblemDetails>(valueResult.Value);
    }

    private static Result CreateMalformedFailureResult(ResultStatus status, ResultError? error)
    {
        var constructor = typeof(Result).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(ResultStatus), typeof(ResultError)],
            modifiers: null);

        Assert.NotNull(constructor);
        return (Result)constructor.Invoke([status, error]);
    }

    private static Result<T> CreateMalformedFailureResult<T>(ResultStatus status, T? value, ResultError? error)
        where T : notnull
    {
        var constructor = typeof(Result<T>).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(ResultStatus), typeof(T), typeof(ResultError)],
            modifiers: null);

        Assert.NotNull(constructor);
        return (Result<T>)constructor.Invoke([status, value, error]);
    }
}
