using SharedKernel.Results;

namespace ViajantesTurismo.Common.UnitTests.Results;

public sealed class ResultErrorTests
{
    [Fact]
    public void Result_error_can_be_created_with_detail_only()
    {
        var error = new ResultError("Something went wrong");

        Assert.Equal("Something went wrong", error.Detail);
        Assert.Null(error.ValidationErrors);
    }

    [Fact]
    public void Result_error_can_be_created_with_validation_errors()
    {
        var validationErrors = new Dictionary<string, string[]>
        {
            ["Name"] = ["Name is required"],
            ["Email"] = ["Email is invalid"]
        };
        var error = new ResultError("Validation failed", ResultErrorCodes.Invalid, validationErrors);

        Assert.Equal("Validation failed", error.Detail);
        Assert.NotNull(error.ValidationErrors);
        Assert.Equal(2, error.ValidationErrors.Count);
        Assert.Equal(["Name is required"], error.ValidationErrors["Name"]);
        Assert.Equal(["Email is invalid"], error.ValidationErrors["Email"]);
    }

    [Fact]
    public void Result_error_equality_works_for_same_values()
    {
        var error1 = new ResultError("Error");
        var error2 = new ResultError("Error");

        Assert.Equal(error1, error2);
    }

    [Fact]
    public void Result_error_equality_fails_for_different_details()
    {
        var error1 = new ResultError("Error 1");
        var error2 = new ResultError("Error 2");

        Assert.NotEqual(error1, error2);
    }

    [Fact]
    public void Result_error_equality_works_with_validation_errors()
    {
        var validationErrors = new Dictionary<string, string[]>
        {
            ["Field"] = ["Message"]
        };
        var error1 = new ResultError("Error", ResultErrorCodes.Invalid, validationErrors);
        var error2 = new ResultError("Error", ResultErrorCodes.Invalid, validationErrors);

        Assert.Equal(error1, error2);
    }

    [Fact]
    public void Result_error_get_hash_code_is_consistent()
    {
        var error = new ResultError("Error");

        var hash1 = error.GetHashCode();
        var hash2 = error.GetHashCode();

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Result_error_to_string_returns_string_representation()
    {
        var error = new ResultError("Error message");

        var str = error.ToString();

        Assert.Contains("Error message", str, StringComparison.Ordinal);
    }
}
