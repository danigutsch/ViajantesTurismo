using ViajantesTurismo.Admin.Web.Exceptions;
using ViajantesTurismo.Admin.Web.Helpers;

namespace ViajantesTurismo.Admin.WebTests.Infrastructure;

public class EditContextValidationHelperApplyValidationErrorsTests
{
    [Fact]
    public void ApplyValidationErrors_When_Exception_Has_No_Field_Errors_Leaves_EditContext_Unchanged()
    {
        // Arrange
        var model = new TestFormModel();
        var editContext = new EditContext(model);
        var serverValidationException = new ApiValidationException("Validation failed", new Dictionary<string, string[]>());
        var validationStateChangedNotifications = 0;

        editContext.OnValidationStateChanged += (_, _) => validationStateChangedNotifications++;

        // Act
        EditContextValidationHelper.ApplyValidationErrors(editContext, serverValidationException);

        // Assert
        Assert.Empty(editContext.GetValidationMessages());
        Assert.Equal(0, validationStateChangedNotifications);
    }

    [Fact]
    public void ApplyValidationErrors_When_One_Field_Has_Multiple_Errors_Associates_All_Messages_With_That_Field()
    {
        // Arrange
        var model = new TestFormModel();
        var editContext = new EditContext(model);
        var serverValidationException = new ApiValidationException("Validation failed", new Dictionary<string, string[]>
        {
            [nameof(TestFormModel.Email)] = ["Email is required.", "Email is invalid."]
        });
        var emailField = editContext.Field(nameof(TestFormModel.Email));
        var validationStateChangedNotifications = 0;

        editContext.OnValidationStateChanged += (_, _) => validationStateChangedNotifications++;

        // Act
        EditContextValidationHelper.ApplyValidationErrors(editContext, serverValidationException);

        // Assert
        var emailMessages = editContext.GetValidationMessages(emailField).ToArray();
        Assert.Equal(["Email is required.", "Email is invalid."], emailMessages);
        Assert.Equal(1, validationStateChangedNotifications);
    }

    [Fact]
    public void ApplyValidationErrors_When_Multiple_Fields_Have_Errors_Associates_Each_Message_With_Its_Field()
    {
        // Arrange
        var model = new TestFormModel();
        var editContext = new EditContext(model);
        var serverValidationException = new ApiValidationException("Validation failed", new Dictionary<string, string[]>
        {
            [nameof(TestFormModel.Email)] = ["Email is invalid."],
            [nameof(TestFormModel.FirstName)] = ["First name is required."]
        });
        var emailField = editContext.Field(nameof(TestFormModel.Email));
        var firstNameField = editContext.Field(nameof(TestFormModel.FirstName));
        var validationStateChangedNotifications = 0;

        editContext.OnValidationStateChanged += (_, _) => validationStateChangedNotifications++;

        // Act
        EditContextValidationHelper.ApplyValidationErrors(editContext, serverValidationException);

        // Assert
        var emailMessages = editContext.GetValidationMessages(emailField).ToArray();
        var firstNameMessages = editContext.GetValidationMessages(firstNameField).ToArray();

        Assert.Equal(["Email is invalid."], emailMessages);
        Assert.Equal(["First name is required."], firstNameMessages);
        Assert.Equal(1, validationStateChangedNotifications);
    }

    private sealed class TestFormModel
    {
        public string Email { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;
    }
}
