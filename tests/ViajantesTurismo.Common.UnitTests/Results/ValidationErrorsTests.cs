using SharedKernel.Results;

namespace ViajantesTurismo.Common.UnitTests.Results;

public sealed class ValidationErrorsTests
{
    [Fact]
    public void Add_accepts_invalid_result()
    {
        var errors = new ValidationErrors();
        var error = Result.Invalid("Validation failed", "Name", "Name is required");

        errors.Add(error);

        Assert.True(errors.HasErrors);
    }

    [Fact]
    public void Add_generic_accepts_invalid_result()
    {
        var errors = new ValidationErrors();
        var error = Result.Invalid<int>("Validation failed", "Age", "Age must be positive");

        errors.Add(error);

        Assert.True(errors.HasErrors);
    }

    [Fact]
    public void Add_throws_when_result_is_not_invalid()
    {
        var errors = new ValidationErrors();
        var successResult = Result.Ok();

        var exception = Assert.Throws<InvalidOperationException>(() => errors.Add(successResult));
        Assert.Contains("Only validation errors can be added", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Add_generic_throws_when_result_is_not_invalid()
    {
        var errors = new ValidationErrors();
        var successResult = Result.Ok(42);

        var exception = Assert.Throws<InvalidOperationException>(() => errors.Add(successResult));
        Assert.Contains("Only validation errors can be added", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void To_result_returns_single_error_when_only_one_error_exists()
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
    public void To_result_generic_returns_single_error_when_only_one_error_exists()
    {
        var errors = new ValidationErrors();
        var singleError = Result.Invalid<int>("Validation failed", "Age", "Age must be positive");
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
    public void To_result_merges_multiple_errors_from_different_fields()
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
    public void To_result_generic_merges_multiple_errors_from_different_fields()
    {
        var errors = new ValidationErrors();
        errors.Add(Result.Invalid<int>("Error 1", "Name", "Name is required"));
        errors.Add(Result.Invalid<int>("Error 2", "Age", "Age must be positive"));

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
    public void To_result_merges_multiple_errors_for_same_field()
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
        Assert.Equal(2, result.ErrorDetails.ValidationErrors["Name"].Count);
        Assert.Contains("Name is required", result.ErrorDetails.ValidationErrors["Name"]);
        Assert.Contains("Name must be at least 3 characters", result.ErrorDetails.ValidationErrors["Name"]);
    }

    [Fact]
    public void Has_errors_returns_true_when_errors_exist()
    {
        var errors = new ValidationErrors();
        errors.Add(Result.Invalid("Error", "Name", "Name is required"));

        Assert.True(errors.HasErrors);
    }

    [Fact]
    public void Has_errors_returns_false_when_no_errors_exist()
    {
        var errors = new ValidationErrors();

        Assert.False(errors.HasErrors);
    }

    [Fact]
    public void To_result_throws_when_no_errors_exist()
    {
        var errors = new ValidationErrors();

        var exception = Assert.Throws<InvalidOperationException>(() => errors.ToResult());
        Assert.Contains("Cannot create result from empty error collection", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void To_result_generic_throws_when_no_errors_exist()
    {
        var errors = new ValidationErrors();

        var exception = Assert.Throws<InvalidOperationException>(() => errors.ToResult<int>());
        Assert.Contains("Cannot create result from empty error collection", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Add_can_mix_generic_and_non_generic_results()
    {
        var errors = new ValidationErrors();
        errors.Add(Result.Invalid("Error 1", "Name", "Name is required"));
        errors.Add(Result.Invalid<int>("Error 2", "Age", "Age must be positive"));

        var result = errors.ToResult();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorDetails);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.Equal(2, result.ErrorDetails.ValidationErrors.Count);
        Assert.Equal(["Name is required"], result.ErrorDetails.ValidationErrors["Name"]);
        Assert.Equal(["Age must be positive"], result.ErrorDetails.ValidationErrors["Age"]);
    }
}
