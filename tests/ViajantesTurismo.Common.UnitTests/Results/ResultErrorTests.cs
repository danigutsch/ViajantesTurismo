using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Common.UnitTests.Results;

public sealed class ResultErrorTests
{
    [Fact]
    public void Result_Error_Can_Be_Created_With_Detail_Only()
    {
        var error = new ResultError("Something went wrong");

        Assert.Equal("Something went wrong", error.Detail);
        Assert.Null(error.ValidationErrors);
    }

    [Fact]
    public void Result_Error_Can_Be_Created_With_Validation_Errors()
    {
        var validationErrors = new Dictionary<string, string[]>
        {
            ["Name"] = ["Name is required"],
            ["Email"] = ["Email is invalid"]
        };
        var error = new ResultError("Validation failed", validationErrors);

        Assert.Equal("Validation failed", error.Detail);
        Assert.NotNull(error.ValidationErrors);
        Assert.Equal(2, error.ValidationErrors.Count);
        Assert.Equal(["Name is required"], error.ValidationErrors["Name"]);
        Assert.Equal(["Email is invalid"], error.ValidationErrors["Email"]);
    }

    [Fact]
    public void Result_Error_Equality_Works_For_Same_Values()
    {
        var error1 = new ResultError("Error");
        var error2 = new ResultError("Error");

        Assert.Equal(error1, error2);
    }

    [Fact]
    public void Result_Error_Equality_Fails_For_Different_Details()
    {
        var error1 = new ResultError("Error 1");
        var error2 = new ResultError("Error 2");

        Assert.NotEqual(error1, error2);
    }

    [Fact]
    public void Result_Error_Equality_Works_With_Validation_Errors()
    {
        var validationErrors = new Dictionary<string, string[]>
        {
            ["Field"] = ["Message"]
        };
        var error1 = new ResultError("Error", validationErrors);
        var error2 = new ResultError("Error", validationErrors);

        Assert.Equal(error1, error2);
    }

    [Fact]
    public void Result_Error_Get_Hash_Code_Is_Consistent()
    {
        var error = new ResultError("Error");

        var hash1 = error.GetHashCode();
        var hash2 = error.GetHashCode();

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Result_Error_To_String_Returns_String_Representation()
    {
        var error = new ResultError("Error message");

        var str = error.ToString();

        Assert.Contains("Error message", str);
    }
}
