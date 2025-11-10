using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Common.UnitTests.Results;

public sealed class ResultTests
{
    [Fact]
    public void Ok_Creates_Successful_Result()
    {
        var result = Result.Ok();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Null(result.ErrorDetails);
    }

    [Fact]
    public void No_Content_Creates_Successful_Result()
    {
        var result = Result.NoContent();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(ResultStatus.NoContent, result.Status);
        Assert.Null(result.ErrorDetails);
    }

    [Fact]
    public void Accepted_Creates_Successful_Result()
    {
        var result = Result.Accepted();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(ResultStatus.Accepted, result.Status);
        Assert.Null(result.ErrorDetails);
    }

    [Fact]
    public void Invalid_Creates_Failed_Result_With_Single_Validation_Error()
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
    public void Invalid_Throws_When_Detail_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Invalid("", "field", "message"));
        Assert.Throws<ArgumentNullException>(() => Result.Invalid(null!, "field", "message"));
        Assert.Throws<ArgumentException>(() => Result.Invalid("   ", "field", "message"));
    }

    [Fact]
    public void Invalid_Throws_When_Field_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Invalid("detail", "", "message"));
        Assert.Throws<ArgumentNullException>(() => Result.Invalid("detail", null!, "message"));
        Assert.Throws<ArgumentException>(() => Result.Invalid("detail", "   ", "message"));
    }

    [Fact]
    public void Invalid_Throws_When_Message_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Invalid("detail", "field", ""));
        Assert.Throws<ArgumentNullException>(() => Result.Invalid("detail", "field", null!));
        Assert.Throws<ArgumentException>(() => Result.Invalid("detail", "field", "   "));
    }

    [Fact]
    public void Invalid_With_Empty_ValidationErrors_Dictionary_Throws_ArgumentOutOfRangeException()
    {
        // Arrange
        var emptyDictionary = new Dictionary<string, string[]>();

        // Act
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Result.Invalid("Some detail", emptyDictionary));
    }

    [Fact]
    public void Not_Found_Creates_Failed_Result()
    {
        var result = Result.NotFound("Resource not found");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Resource not found", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Not_Found_Throws_When_Detail_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => Result.NotFound(""));
        Assert.Throws<ArgumentNullException>(() => Result.NotFound(null!));
        Assert.Throws<ArgumentException>(() => Result.NotFound("   "));
    }

    [Fact]
    public void Unauthorized_Creates_Failed_Result()
    {
        var result = Result.Unauthorized("Access denied");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Access denied", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Unauthorized_Throws_When_Detail_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Unauthorized(""));
        Assert.Throws<ArgumentNullException>(() => Result.Unauthorized(null!));
        Assert.Throws<ArgumentException>(() => Result.Unauthorized("   "));
    }

    [Fact]
    public void Forbidden_Creates_Failed_Result()
    {
        var result = Result.Forbidden("Operation forbidden");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Operation forbidden", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Forbidden_Throws_When_Detail_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Forbidden(""));
        Assert.Throws<ArgumentNullException>(() => Result.Forbidden(null!));
        Assert.Throws<ArgumentException>(() => Result.Forbidden("   "));
    }

    [Fact]
    public void Conflict_Creates_Failed_Result()
    {
        var result = Result.Conflict("Resource conflict");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Resource conflict", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Conflict_Throws_When_Detail_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Conflict(""));
        Assert.Throws<ArgumentNullException>(() => Result.Conflict(null!));
        Assert.Throws<ArgumentException>(() => Result.Conflict("   "));
    }

    [Fact]
    public void Error_Creates_Failed_Result()
    {
        var result = Result.Error("An error occurred");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("An error occurred", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Error_Throws_When_Detail_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Error(""));
        Assert.Throws<ArgumentNullException>(() => Result.Error(null!));
        Assert.Throws<ArgumentException>(() => Result.Error("   "));
    }

    [Fact]
    public void Critical_Error_Creates_Failed_Result()
    {
        var result = Result.CriticalError("Critical failure");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.CriticalError, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Critical failure", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Critical_Error_Throws_When_Detail_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => Result.CriticalError(""));
        Assert.Throws<ArgumentNullException>(() => Result.CriticalError(null!));
        Assert.Throws<ArgumentException>(() => Result.CriticalError("   "));
    }

    [Fact]
    public void Unavailable_Creates_Failed_Result()
    {
        var result = Result.Unavailable("Service unavailable");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Unavailable, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Service unavailable", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Unavailable_Throws_When_Detail_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Unavailable(""));
        Assert.Throws<ArgumentNullException>(() => Result.Unavailable(null!));
        Assert.Throws<ArgumentException>(() => Result.Unavailable("   "));
    }

    [Fact]
    public void Equality_Works_For_Success_Results()
    {
        var result1 = Result.Ok();
        var result2 = Result.Ok();

        Assert.Equal(result1, result2);
        Assert.True(result1 == result2);
        Assert.False(result1 != result2);
    }

    [Fact]
    public void Equality_Works_For_Failed_Results_With_Same_Error()
    {
        var result1 = Result.NotFound("Not found");
        var result2 = Result.NotFound("Not found");

        Assert.Equal(result1, result2);
        Assert.True(result1 == result2);
        Assert.False(result1 != result2);
    }

    [Fact]
    public void Equality_Fails_For_Results_With_Different_Errors()
    {
        var result1 = Result.NotFound("Not found");
        var result2 = Result.Error("Error");

        Assert.NotEqual(result1, result2);
        Assert.False(result1 == result2);
        Assert.True(result1 != result2);
    }

    [Fact]
    public void Equality_Fails_For_Results_With_Different_Error_Messages()
    {
        var result1 = Result.NotFound("Message 1");
        var result2 = Result.NotFound("Message 2");

        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void Get_Hash_Code_Is_Consistent()
    {
        var result = Result.Ok();

        var hash1 = result.GetHashCode();
        var hash2 = result.GetHashCode();

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void To_String_Returns_Success_Status_For_Success()
    {
        var result = Result.Ok();

        var str = result.ToString();

        Assert.Contains("Success", str);
        Assert.Contains("Ok", str);
    }

    [Fact]
    public void To_String_Returns_Failure_Status_And_Error_For_Failure()
    {
        var result = Result.Error("Something went wrong");

        var str = result.ToString();

        Assert.Contains("Failure", str);
        Assert.Contains("Error", str);
        Assert.Contains("Something went wrong", str);
    }

    [Fact]
    public void Equals_Object_Returns_False_For_Non_Result_Object()
    {
        var result = Result.Ok();

        Assert.False(result.Equals(new object()));
        Assert.False(result.Equals(null));
        // ReSharper disable once SuspiciousTypeConversion.Global
        Assert.False(result.Equals("string"));
        // ReSharper disable once SuspiciousTypeConversion.Global
        Assert.False(result.Equals(42));
    }

    [Fact]
    public void Equals_Object_Returns_True_For_Boxed_Equal_Result()
    {
        var result1 = Result.Ok();
        object result2 = Result.Ok();

        Assert.True(result1.Equals(result2));
    }

    [Fact]
    public void Different_Success_Statuses_Are_Not_Equal()
    {
        var ok = Result.Ok();
        var noContent = Result.NoContent();
        var accepted = Result.Accepted();

        Assert.NotEqual(ok, noContent);
        Assert.NotEqual(ok, accepted);
        Assert.NotEqual(noContent, accepted);
    }

    [Fact]
    public void Success_Result_Error_Details_Returns_Null()
    {
        var result = Result.Ok();

        Assert.Null(result.ErrorDetails);
    }
}
