namespace SharedKernel.Results.Tests;

[Trait(TestTraits.CapabilityName, TestTraits.ResultCapability)]
[Trait(TestTraits.CategoryName, TestTraits.CoreBehaviorCategory)]
public sealed class ValidationErrorsTests
{
    [Fact]
    public void Adds_Invalid_Results_And_Reports_HasErrors()
    {
        // Arrange
        var errors = new ValidationErrors();

        // Act
        errors.Add(Result.Invalid("Validation failed", "Name", "Name is required"));

        // Assert
        Assert.True(errors.HasErrors);
    }

    [Fact]
    public void Adds_Generic_Invalid_Results_And_Reports_HasErrors()
    {
        // Arrange
        var errors = new ValidationErrors();

        // Act
        errors.Add(Result.Invalid<int>("Validation failed", "Age", "Age must be positive"));

        // Assert
        Assert.True(errors.HasErrors);
    }

    [Fact]
    public void Merges_Multiple_Errors_Into_A_Single_Result()
    {
        // Arrange
        var errors = new ValidationErrors();
        errors.Add(Result.Invalid("Validation failed", "Name", "Name is required"));
        errors.Add(Result.Invalid("Validation failed", "Email", "Email is invalid"));

        // Act
        var result = errors.ToResult();

        // Assert
        Assert.Equal(ResultStatus.Invalid, result.Status);
        var error = result.ErrorDetails;
        Assert.NotNull(error);
        Assert.Equal(ResultErrorCodes.Invalid, error.Code);
        Assert.Equal("Multiple validation errors occurred.", error.Detail);
        Assert.NotNull(error.ValidationErrors);
        Assert.Equal(["Name is required"], error.ValidationErrors["Name"]);
        Assert.Equal(["Email is invalid"], error.ValidationErrors["Email"]);
    }

    [Fact]
    public void Merges_Multiple_Errors_For_The_Same_Field()
    {
        // Arrange
        var errors = new ValidationErrors();
        errors.Add(Result.Invalid("Validation failed", "Name", "Name is required"));
        errors.Add(Result.Invalid("Validation failed", "Name", "Name must be at least 3 characters"));

        // Act
        var result = errors.ToResult();

        // Assert
        var error = result.ErrorDetails;
        Assert.NotNull(error);
        Assert.NotNull(error.ValidationErrors);
        Assert.Equal(
            ["Name is required", "Name must be at least 3 characters"],
            error.ValidationErrors["Name"]);
    }

    [Fact]
    public void Converts_A_Single_Error_To_A_Generic_Result()
    {
        // Arrange
        var errors = new ValidationErrors();
        errors.Add(Result.Invalid("Validation failed", "Age", "Age must be positive"));

        // Act
        var result = errors.ToResult<int>();

        // Assert
        Assert.Equal(ResultStatus.Invalid, result.Status);
        var error = result.ErrorDetails;
        Assert.NotNull(error);
        Assert.Equal(ResultErrorCodes.Invalid, error.Code);
        Assert.NotNull(error.ValidationErrors);
        Assert.Equal(["Age must be positive"], error.ValidationErrors["Age"]);
    }

    [Fact]
    public void Throws_When_Converting_An_Empty_Collection()
    {
        // Arrange
        var errors = new ValidationErrors();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => errors.ToResult());

        // Assert
        Assert.Contains("Cannot create result from empty error collection", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_Non_Invalid_Results()
    {
        // Arrange
        var errors = new ValidationErrors();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => errors.Add(Result.Ok()));

        // Assert
        Assert.Contains("Only validation errors can be added", exception.Message, StringComparison.Ordinal);
    }
}
