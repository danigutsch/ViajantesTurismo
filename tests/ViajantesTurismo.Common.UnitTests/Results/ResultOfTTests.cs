using SharedKernel.Functional;

namespace ViajantesTurismo.Common.UnitTests.Results;

public sealed class ResultOfTTests
{
    [Fact]
    public void Ok_Creates_Successful_Result_With_Value()
    {
        var result = Result.Ok(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(42, result.Value);
        Assert.Null(result.ErrorDetails);
    }

    [Fact]
    public void Ok_Throws_When_Value_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Ok<string>(null!));
    }

    [Fact]
    public void Created_Creates_Successful_Result_With_Value()
    {
        var result = Result.Created("test");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(ResultStatus.Created, result.Status);
        Assert.Equal("test", result.Value);
        Assert.Null(result.ErrorDetails);
    }

    [Fact]
    public void Created_Throws_When_Value_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Created<string>(null!));
    }

    [Fact]
    public void Accepted_Creates_Successful_Result_With_Value()
    {
        var result = Result.Accepted(true);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(ResultStatus.Accepted, result.Status);
        Assert.True(result.Value);
        Assert.Null(result.ErrorDetails);
    }

    [Fact]
    public void Accepted_Throws_When_Value_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Accepted<string>(null!));
    }

    [Fact]
    public void Invalid_Creates_Failed_Result_With_Single_Validation_Error()
    {
        var result = Result.Invalid<int>("Validation failed", "Age", "Age must be positive");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Validation failed", result.ErrorDetails.Detail);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.Single(result.ErrorDetails.ValidationErrors);
        Assert.Equal(["Age must be positive"], result.ErrorDetails.ValidationErrors["Age"]);
    }

    [Fact]
    public void Invalid_Throws_When_Detail_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Invalid<int>("", "field", "message"));
        Assert.Throws<ArgumentNullException>(() => Result.Invalid<int>(null!, "field", "message"));
        Assert.Throws<ArgumentException>(() => Result.Invalid<int>("   ", "field", "message"));
    }

    [Fact]
    public void Invalid_Throws_When_Field_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Invalid<int>("detail", "", "message"));
        Assert.Throws<ArgumentNullException>(() => Result.Invalid<int>("detail", null!, "message"));
        Assert.Throws<ArgumentException>(() => Result.Invalid<int>("detail", "   ", "message"));
    }

    [Fact]
    public void Invalid_Throws_When_Message_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Invalid<int>("detail", "field", ""));
        Assert.Throws<ArgumentNullException>(() => Result.Invalid<int>("detail", "field", null!));
        Assert.Throws<ArgumentException>(() => Result.Invalid<int>("detail", "field", "   "));
    }

    [Fact]
    public void Not_Found_Creates_Failed_Result()
    {
        var result = Result.NotFound<string>("User not found");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("User not found", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Not_Found_Throws_When_Detail_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => Result.NotFound<int>(""));
        Assert.Throws<ArgumentNullException>(() => Result.NotFound<int>(null!));
        Assert.Throws<ArgumentException>(() => Result.NotFound<int>("   "));
    }

    [Fact]
    public void Unauthorized_Creates_Failed_Result()
    {
        var result = Result.Unauthorized<string>("Access denied");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Access denied", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Unauthorized_Throws_When_Detail_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Unauthorized<int>(""));
        Assert.Throws<ArgumentNullException>(() => Result.Unauthorized<int>(null!));
        Assert.Throws<ArgumentException>(() => Result.Unauthorized<int>("   "));
    }

    [Fact]
    public void Forbidden_Creates_Failed_Result()
    {
        var result = Result.Forbidden<string>("Operation forbidden");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Operation forbidden", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Forbidden_Throws_When_Detail_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Forbidden<int>(""));
        Assert.Throws<ArgumentNullException>(() => Result.Forbidden<int>(null!));
        Assert.Throws<ArgumentException>(() => Result.Forbidden<int>("   "));
    }

    [Fact]
    public void Conflict_Creates_Failed_Result()
    {
        var result = Result.Conflict<string>("Resource conflict");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Resource conflict", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Conflict_Throws_When_Detail_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Conflict<int>(""));
        Assert.Throws<ArgumentNullException>(() => Result.Conflict<int>(null!));
        Assert.Throws<ArgumentException>(() => Result.Conflict<int>("   "));
    }

    [Fact]
    public void Error_Creates_Failed_Result()
    {
        var result = Result.Error<string>("An error occurred");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("An error occurred", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Error_Throws_When_Detail_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Error<int>(""));
        Assert.Throws<ArgumentNullException>(() => Result.Error<int>(null!));
        Assert.Throws<ArgumentException>(() => Result.Error<int>("   "));
    }

    [Fact]
    public void Critical_Error_Creates_Failed_Result()
    {
        var result = Result.CriticalError<string>("Critical failure");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.CriticalError, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Critical failure", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Critical_Error_Throws_When_Detail_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => Result.CriticalError<int>(""));
        Assert.Throws<ArgumentNullException>(() => Result.CriticalError<int>(null!));
        Assert.Throws<ArgumentException>(() => Result.CriticalError<int>("   "));
    }

    [Fact]
    public void Unavailable_Creates_Failed_Result()
    {
        var result = Result.Unavailable<string>("Service unavailable");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Unavailable, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Service unavailable", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Unavailable_Throws_When_Detail_Is_Null_Or_Empty()
    {
        Assert.Throws<ArgumentException>(() => Result.Unavailable<int>(""));
        Assert.Throws<ArgumentNullException>(() => Result.Unavailable<int>(null!));
        Assert.Throws<ArgumentException>(() => Result.Unavailable<int>("   "));
    }

    [Fact]
    public void Value_Throws_Invalid_Operation_Exception_When_Accessing_Failed_Result()
    {
        var result = Result.Error<int>("Failed");

        var exception = Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Contains("Cannot access Value of a failed result", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Failed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Implicit_Conversion_From_Value_Creates_Success_Result()
    {
        Result<int> result = Result.Ok(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Equal(ResultStatus.Ok, result.Status);
    }

    [Fact]
    public void To_Result_Converts_Success_Result_To_Non_Generic()
    {
        var result = Result.Ok(42);

        var nonGeneric = result.ToResult();

        Assert.True(nonGeneric.IsSuccess);
        Assert.Equal(ResultStatus.Ok, nonGeneric.Status);
        Assert.Null(nonGeneric.ErrorDetails);
    }

    [Fact]
    public void To_Result_Converts_Failed_Result_To_Non_Generic()
    {
        var result = Result.Error<int>("Failed");

        var nonGeneric = result.ToResult();

        Assert.False(nonGeneric.IsSuccess);
        Assert.Equal(ResultStatus.Error, nonGeneric.Status);
        Assert.NotNull(nonGeneric.ErrorDetails);
        Assert.Equal("Failed", nonGeneric.ErrorDetails.Detail);
    }

    [Fact]
    public void Equality_Works_For_Success_Results_With_Same_Value()
    {
        var result1 = Result.Ok(42);
        var result2 = Result.Ok(42);

        Assert.Equal(result1, result2);
        Assert.True(result1 == result2);
        Assert.False(result1 != result2);
    }

    [Fact]
    public void Equality_Fails_For_Success_Results_With_Different_Values()
    {
        var result1 = Result.Ok(42);
        var result2 = Result.Ok(43);

        Assert.NotEqual(result1, result2);
        Assert.False(result1 == result2);
        Assert.True(result1 != result2);
    }

    [Fact]
    public void Equality_Works_For_Failed_Results_With_Same_Error()
    {
        var result1 = Result.NotFound<int>("Not found");
        var result2 = Result.NotFound<int>("Not found");

        Assert.Equal(result1, result2);
        Assert.True(result1 == result2);
        Assert.False(result1 != result2);
    }

    [Fact]
    public void Equality_Fails_For_Failed_Results_With_Different_Errors()
    {
        var result1 = Result.NotFound<int>("Not found");
        var result2 = Result.Error<int>("Error");

        Assert.NotEqual(result1, result2);
        Assert.False(result1 == result2);
        Assert.True(result1 != result2);
    }

    [Fact]
    public void Equality_Fails_For_Success_And_Failure_Results()
    {
        var result1 = Result.Ok(42);
        var result2 = Result.Error<int>("Failed");

        Assert.NotEqual(result1, result2);
        Assert.False(result1 == result2);
        Assert.True(result1 != result2);
    }

    [Fact]
    public void Get_Hash_Code_Is_Consistent()
    {
        var result = Result.Ok(42);

        var hash1 = result.GetHashCode();
        var hash2 = result.GetHashCode();

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Get_Hash_Code_Differs_For_Different_Values()
    {
        var result1 = Result.Ok(42);
        var result2 = Result.Ok(43);

        Assert.NotEqual(result1.GetHashCode(), result2.GetHashCode());
    }

    [Fact]
    public void To_String_Returns_Success_Status_And_Value_For_Success()
    {
        var result = Result.Ok(42);

        var str = result.ToString();

        Assert.Contains("Success", str, StringComparison.Ordinal);
        Assert.Contains("Ok", str, StringComparison.Ordinal);
        Assert.Contains("42", str, StringComparison.Ordinal);
    }

    [Fact]
    public void To_String_Returns_Failure_Status_And_Error_For_Failure()
    {
        var result = Result.Error<string>("Something went wrong");

        var str = result.ToString();

        Assert.Contains("Failure", str, StringComparison.Ordinal);
        Assert.Contains("Error", str, StringComparison.Ordinal);
        Assert.Contains("Something went wrong", str, StringComparison.Ordinal);
    }

    [Fact]
    public void Different_Success_Statuses_Are_Not_Equal()
    {
        var result1 = Result.Ok(42);
        var result2 = Result.Created(42);

        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void Success_Result_With_Reference_Type_Stores_Value()
    {
        var list = new List<int> { 1, 2, 3 };
        var result = Result.Ok(list);

        Assert.Same(list, result.Value);
    }

    [Fact]
    public void Equals_Object_Returns_False_For_Non_Result_Object()
    {
        var result = Result.Ok(42);

        Assert.False(result.Equals(new object()));
        Assert.False(result.Equals(null));
        // ReSharper disable once SuspiciousTypeConversion.Global
        Assert.False(result.Equals("string"));
    }

    [Fact]
    public void Equals_Object_Returns_True_For_Boxed_Equal_Result()
    {
        var result1 = Result.Ok(42);
        object result2 = Result.Ok(42);

        Assert.True(result1.Equals(result2));
    }

    [Fact]
    public void Success_Result_Error_Details_Returns_Null()
    {
        var result = Result.Ok(42);

        Assert.Null(result.ErrorDetails);
    }

    [Fact]
    public void Failed_Result_Value_Access_Shows_Error_Detail_In_Exception()
    {
        var result = Result.NotFound<int>("Resource not found");

        var exception = Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Contains("Resource not found", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Implicit_Conversion_Throws_For_Null_Value()
    {
        string? nullString = null;

        Assert.Throws<ArgumentNullException>(() =>
        {
            Result<string> _ = Result.Ok(nullString!);
        });
    }
}
