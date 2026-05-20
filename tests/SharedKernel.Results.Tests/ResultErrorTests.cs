namespace SharedKernel.Results.Tests;

[Trait(TestTraits.CapabilityName, TestTraits.ResultCapability)]
[Trait(TestTraits.CategoryName, TestTraits.CoreBehaviorCategory)]
public sealed class ResultErrorTests
{
    [Fact]
    public void Creates_An_Error_With_Detail_And_Default_Code()
    {
        // Arrange
        // Act
        var error = new ResultError("Something went wrong");

        // Assert
        Assert.Equal(ResultErrorCodes.Error, error.Code);
        Assert.Equal("Something went wrong", error.Detail);
        Assert.Null(error.ValidationErrors);
    }

    [Fact]
    public void Creates_An_Error_With_A_Specific_Code()
    {
        // Arrange
        // Act
        var error = new ResultError("Tour not found", ResultErrorCodes.NotFound);

        // Assert
        Assert.Equal(ResultErrorCodes.NotFound, error.Code);
        Assert.Equal("Tour not found", error.Detail);
    }

    [Fact]
    public void Defensively_Copies_Validation_Errors()
    {
        // Arrange
        var source = new Dictionary<string, string[]>
        {
            ["Name"] = ["Name is required"],
        };

        // Act
        var error = new ResultError("Validation failed", ResultErrorCodes.Invalid, source);
        source["Name"] = ["Changed after construction"];

        // Assert
        Assert.NotNull(error.ValidationErrors);
        Assert.Equal(["Name is required"], error.ValidationErrors["Name"]);
    }

    [Fact]
    public void Exposes_Read_Only_Validation_Error_Collections()
    {
        // Arrange
        var error = new ResultError(
            "Validation failed",
            ResultErrorCodes.Invalid,
            new Dictionary<string, string[]>
            {
                ["Name"] = ["Name is required"],
            });

        Assert.NotNull(error.ValidationErrors);

        // Act
        var messages = error.ValidationErrors["Name"];

        // Assert
        Assert.IsNotType<List<string>>(messages);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)messages).Add("Changed after construction"));
        Assert.Equal(["Name is required"], error.ValidationErrors["Name"]);
    }

    [Fact]
    public void Uses_Code_And_Detail_In_ToString()
    {
        // Arrange
        var error = new ResultError("Tour not found", ResultErrorCodes.NotFound);

        // Act
        var text = error.ToString();

        // Assert
        Assert.Equal("not_found: Tour not found", text);
    }
}
