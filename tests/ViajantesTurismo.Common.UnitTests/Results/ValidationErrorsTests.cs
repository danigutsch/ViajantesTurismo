using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Common.UnitTests.Results;

public sealed class ValidationErrorsTests
{
    [Fact]
    public void Add_Accepts_Invalid_Result()
    {
        var errors = new ValidationErrors();
        var error = Result.Invalid("Validation failed", "Name", "Name is required");

        errors.Add(error);

        Assert.True(errors.HasErrors);
    }

    [Fact]
    public void Add_Generic_Accepts_Invalid_Result()
    {
        var errors = new ValidationErrors();
        var error = Result<int>.Invalid("Validation failed", "Age", "Age must be positive");

        errors.Add(error);

        Assert.True(errors.HasErrors);
    }

    [Fact]
    public void Add_Throws_When_Result_Is_Not_Invalid()
    {
        var errors = new ValidationErrors();
        var successResult = Result.Ok();

        var exception = Assert.Throws<InvalidOperationException>(() => errors.Add(successResult));
        Assert.Contains("Only validation errors can be added", exception.Message);
    }

    [Fact]
    public void Add_Generic_Throws_When_Result_Is_Not_Invalid()
    {
        var errors = new ValidationErrors();
        var successResult = Result<int>.Ok(42);

        var exception = Assert.Throws<InvalidOperationException>(() => errors.Add(successResult));
        Assert.Contains("Only validation errors can be added", exception.Message);
    }

    [Fact]
    public void ToResult_Returns_Single_Error_When_Only_One_Error_Exists()
    {
        var errors = new ValidationErrors();
        var singleError = Result.Invalid("Validation failed", "Name", "Name is required");
        errors.Add(singleError);

        var result = errors.ToResult();

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Validation failed", result.ErrorDetails.Detail);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.Single(result.ErrorDetails.ValidationErrors);
        Assert.Equal(["Name is required"], result.ErrorDetails.ValidationErrors["Name"]);
    }

    [Fact]
    public void ToResult_Generic_Returns_Single_Error_When_Only_One_Error_Exists()
    {
        var errors = new ValidationErrors();
        var singleError = Result<int>.Invalid("Validation failed", "Age", "Age must be positive");
        errors.Add(singleError);

        var result = errors.ToResult<int>();

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Validation failed", result.ErrorDetails.Detail);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.Single(result.ErrorDetails.ValidationErrors);
        Assert.Equal(["Age must be positive"], result.ErrorDetails.ValidationErrors["Age"]);
    }

    [Fact]
    public void ToResult_Merges_Multiple_Errors_From_Different_Fields()
    {
        var errors = new ValidationErrors();
        errors.Add(Result.Invalid("Error 1", "Name", "Name is required"));
        errors.Add(Result.Invalid("Error 2", "Email", "Email is invalid"));

        var result = errors.ToResult();

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Multiple validation errors occurred.", result.ErrorDetails.Detail);
        Assert.Equal(2, result.ErrorDetails.ValidationErrors!.Count);
        Assert.Equal(["Name is required"], result.ErrorDetails.ValidationErrors["Name"]);
        Assert.Equal(["Email is invalid"], result.ErrorDetails.ValidationErrors["Email"]);
    }

    [Fact]
    public void ToResult_Generic_Merges_Multiple_Errors_From_Different_Fields()
    {
        var errors = new ValidationErrors();
        errors.Add(Result<int>.Invalid("Error 1", "Name", "Name is required"));
        errors.Add(Result<int>.Invalid("Error 2", "Age", "Age must be positive"));

        var result = errors.ToResult<int>();

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Multiple validation errors occurred.", result.ErrorDetails.Detail);
        Assert.Equal(2, result.ErrorDetails.ValidationErrors!.Count);
        Assert.Equal(["Name is required"], result.ErrorDetails.ValidationErrors["Name"]);
        Assert.Equal(["Age must be positive"], result.ErrorDetails.ValidationErrors["Age"]);
    }

    [Fact]
    public void ToResult_Merges_Multiple_Errors_For_Same_Field()
    {
        var errors = new ValidationErrors();
        errors.Add(Result.Invalid("Error 1", "Name", "Name is required"));
        errors.Add(Result.Invalid("Error 2", "Name", "Name must be at least 3 characters"));

        var result = errors.ToResult();

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal("Multiple validation errors occurred.", result.ErrorDetails.Detail);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.Single(result.ErrorDetails.ValidationErrors);
        Assert.Equal(2, result.ErrorDetails.ValidationErrors["Name"].Length);
        Assert.Contains("Name is required", result.ErrorDetails.ValidationErrors["Name"]);
        Assert.Contains("Name must be at least 3 characters", result.ErrorDetails.ValidationErrors["Name"]);
    }

    [Fact]
    public void HasErrors_Returns_True_When_Errors_Exist()
    {
        var errors = new ValidationErrors();
        errors.Add(Result.Invalid("Error", "Name", "Name is required"));

        Assert.True(errors.HasErrors);
    }

    [Fact]
    public void HasErrors_Returns_False_When_No_Errors_Exist()
    {
        var errors = new ValidationErrors();

        Assert.False(errors.HasErrors);
    }

    [Fact]
    public void ToResult_Throws_When_No_Errors_Exist()
    {
        var errors = new ValidationErrors();

        var exception = Assert.Throws<InvalidOperationException>(() => errors.ToResult());
        Assert.Contains("Cannot create result from empty error collection", exception.Message);
    }

    [Fact]
    public void ToResult_Generic_Throws_When_No_Errors_Exist()
    {
        var errors = new ValidationErrors();

        var exception = Assert.Throws<InvalidOperationException>(() => errors.ToResult<int>());
        Assert.Contains("Cannot create result from empty error collection", exception.Message);
    }

    [Fact]
    public void Add_Can_Mix_Generic_And_Non_Generic_Results()
    {
        var errors = new ValidationErrors();
        errors.Add(Result.Invalid("Error 1", "Name", "Name is required"));
        errors.Add(Result<int>.Invalid("Error 2", "Age", "Age must be positive"));

        var result = errors.ToResult();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorDetails);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.Equal(2, result.ErrorDetails.ValidationErrors.Count);
        Assert.Equal(["Name is required"], result.ErrorDetails.ValidationErrors["Name"]);
        Assert.Equal(["Age must be positive"], result.ErrorDetails.ValidationErrors["Age"]);
    }
}
