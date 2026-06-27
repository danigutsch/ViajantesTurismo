using ViajantesTurismo.Management.Web.Exceptions;
using ViajantesTurismo.Management.Web.Helpers;

namespace ViajantesTurismo.Management.WebTests.Infrastructure;

public class EditContextValidationHelperApplyValidationErrorsTests
{
    [Fact]
    public void ApplyValidationErrors_when_exception_has_no_field_errors_leaves_editContext_unchanged()
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
    public void ApplyValidationErrors_when_one_field_has_multiple_errors_associates_all_messages_with_that_field()
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
    public void ApplyValidationErrors_when_multiple_fields_have_errors_associates_each_message_with_its_field()
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

}
