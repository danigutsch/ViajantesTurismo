using SharedKernel.Results;

namespace ViajantesTurismo.Common.UnitTests.Results;

public sealed class ResultTests
{
    [Fact]
    public void Ok_creates_successful_result()
    {
        var result = Result.Ok();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Null(result.ErrorDetails);
    }

    [Fact]
    public void No_content_creates_successful_result()
    {
        var result = Result.NoContent();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(ResultStatus.NoContent, result.Status);
        Assert.Null(result.ErrorDetails);
    }

    [Fact]
    public void Accepted_creates_successful_result()
    {
        var result = Result.Accepted();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(ResultStatus.Accepted, result.Status);
        Assert.Null(result.ErrorDetails);
    }

    [Fact]
    public void Invalid_creates_failed_result_with_single_validation_error()
    {
        var result = Result.Invalid("Validation failed", "Name", "Name is required");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Validation failed", result.ErrorDetails.Detail);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.Single(result.ErrorDetails.ValidationErrors);
        Assert.Equal(["Name is required"], result.ErrorDetails.ValidationErrors["Name"]);
    }

    [Fact]
    public void Invalid_throws_when_detail_is_null_or_empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Invalid("", "field", "message"));
        Assert.Throws<ArgumentNullException>(() => Result.Invalid(null!, "field", "message"));
        Assert.Throws<ArgumentException>(() => Result.Invalid("   ", "field", "message"));
    }

    [Fact]
    public void Invalid_throws_when_field_is_null_or_empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Invalid("detail", "", "message"));
        Assert.Throws<ArgumentNullException>(() => Result.Invalid("detail", null!, "message"));
        Assert.Throws<ArgumentException>(() => Result.Invalid("detail", "   ", "message"));
    }

    [Fact]
    public void Invalid_throws_when_message_is_null_or_empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Invalid("detail", "field", ""));
        Assert.Throws<ArgumentNullException>(() => Result.Invalid("detail", "field", null!));
        Assert.Throws<ArgumentException>(() => Result.Invalid("detail", "field", "   "));
    }

    [Fact]
    public void Invalid_with_empty_validationerrors_dictionary_throws_argumentoutofrangeexception()
    {
        // Arrange
        var emptyDictionary = new Dictionary<string, string[]>();

        // Act
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Result.Invalid("Some detail", emptyDictionary));
    }

    [Fact]
    public void Not_found_creates_failed_result()
    {
        var result = Result.NotFound("Resource not found");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Resource not found", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Not_found_throws_when_detail_is_null_or_empty()
    {
        Assert.Throws<ArgumentException>(() => Result.NotFound(""));
        Assert.Throws<ArgumentNullException>(() => Result.NotFound(null!));
        Assert.Throws<ArgumentException>(() => Result.NotFound("   "));
    }

    [Fact]
    public void Unauthorized_creates_failed_result()
    {
        var result = Result.Unauthorized("Access denied");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Access denied", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Unauthorized_throws_when_detail_is_null_or_empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Unauthorized(""));
        Assert.Throws<ArgumentNullException>(() => Result.Unauthorized(null!));
        Assert.Throws<ArgumentException>(() => Result.Unauthorized("   "));
    }

    [Fact]
    public void Forbidden_creates_failed_result()
    {
        var result = Result.Forbidden("Operation forbidden");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Operation forbidden", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Forbidden_throws_when_detail_is_null_or_empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Forbidden(""));
        Assert.Throws<ArgumentNullException>(() => Result.Forbidden(null!));
        Assert.Throws<ArgumentException>(() => Result.Forbidden("   "));
    }

    [Fact]
    public void Conflict_creates_failed_result()
    {
        var result = Result.Conflict("Resource conflict");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Resource conflict", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Conflict_throws_when_detail_is_null_or_empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Conflict(""));
        Assert.Throws<ArgumentNullException>(() => Result.Conflict(null!));
        Assert.Throws<ArgumentException>(() => Result.Conflict("   "));
    }

    [Fact]
    public void Error_creates_failed_result()
    {
        var result = Result.Error("An error occurred");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("An error occurred", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Error_throws_when_detail_is_null_or_empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Error(""));
        Assert.Throws<ArgumentNullException>(() => Result.Error(null!));
        Assert.Throws<ArgumentException>(() => Result.Error("   "));
    }

    [Fact]
    public void Critical_error_creates_failed_result()
    {
        var result = Result.CriticalError("Critical failure");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.CriticalError, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Critical failure", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Critical_error_throws_when_detail_is_null_or_empty()
    {
        Assert.Throws<ArgumentException>(() => Result.CriticalError(""));
        Assert.Throws<ArgumentNullException>(() => Result.CriticalError(null!));
        Assert.Throws<ArgumentException>(() => Result.CriticalError("   "));
    }

    [Fact]
    public void Unavailable_creates_failed_result()
    {
        var result = Result.Unavailable("Service unavailable");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Unavailable, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Service unavailable", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Unavailable_throws_when_detail_is_null_or_empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Unavailable(""));
        Assert.Throws<ArgumentNullException>(() => Result.Unavailable(null!));
        Assert.Throws<ArgumentException>(() => Result.Unavailable("   "));
    }

    [Fact]
    public void Equality_works_for_success_results()
    {
        var result1 = Result.Ok();
        var result2 = Result.Ok();

        Assert.Equal(result1, result2);
        Assert.True(result1 == result2);
        Assert.False(result1 != result2);
    }

    [Fact]
    public void Equality_works_for_failed_results_with_same_error()
    {
        var result1 = Result.NotFound("Not found");
        var result2 = Result.NotFound("Not found");

        Assert.Equal(result1, result2);
        Assert.True(result1 == result2);
        Assert.False(result1 != result2);
    }

    [Fact]
    public void Equality_fails_for_results_with_different_errors()
    {
        var result1 = Result.NotFound("Not found");
        var result2 = Result.Error("Error");

        Assert.NotEqual(result1, result2);
        Assert.False(result1 == result2);
        Assert.True(result1 != result2);
    }

    [Fact]
    public void Equality_fails_for_results_with_different_error_messages()
    {
        var result1 = Result.NotFound("Message 1");
        var result2 = Result.NotFound("Message 2");

        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void Get_hash_code_is_consistent()
    {
        var result = Result.Ok();

        var hash1 = result.GetHashCode();
        var hash2 = result.GetHashCode();

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void To_string_returns_success_status_for_success()
    {
        var result = Result.Ok();

        var str = result.ToString();

        Assert.Contains("Success", str, StringComparison.Ordinal);
        Assert.Contains("Ok", str, StringComparison.Ordinal);
    }

    [Fact]
    public void To_string_returns_failure_status_and_error_for_failure()
    {
        var result = Result.Error("Something went wrong");

        var str = result.ToString();

        Assert.Contains("Failure", str, StringComparison.Ordinal);
        Assert.Contains("Error", str, StringComparison.Ordinal);
        Assert.Contains("Something went wrong", str, StringComparison.Ordinal);
    }

    [Fact]
    public void Equals_object_returns_false_for_non_result_object()
    {
        var result = Result.Ok();

        Assert.False(result.Equals(new object()));
        Assert.False(result.Equals(null));
    }

    [Fact]
    public void Equals_object_returns_true_for_boxed_equal_result()
    {
        var result1 = Result.Ok();
        object result2 = Result.Ok();

        Assert.True(result1.Equals(result2));
    }

    [Fact]
    public void Different_success_statuses_are_not_equal()
    {
        var ok = Result.Ok();
        var noContent = Result.NoContent();
        var accepted = Result.Accepted();

        Assert.NotEqual(ok, noContent);
        Assert.NotEqual(ok, accepted);
        Assert.NotEqual(noContent, accepted);
    }

    [Fact]
    public void Success_result_error_details_returns_null()
    {
        var result = Result.Ok();

        Assert.Null(result.ErrorDetails);
    }
}
