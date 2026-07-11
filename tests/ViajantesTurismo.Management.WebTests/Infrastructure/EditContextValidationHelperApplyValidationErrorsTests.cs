using SharedKernel.HttpClients;
using ViajantesTurismo.Management.Web.Helpers;

namespace ViajantesTurismo.Management.WebTests.Infrastructure;

public class EditContextValidationHelperApplyValidationErrorsTests
{
    [Fact]
    public void ApplyValidationErrors_when_exception_has_no_field_errors_leaves_EditContext_unchanged()
    {
        // Arrange
        var model = new TestFormModel();
        var editContext = new EditContext(model);
        var serverValidationException = new ContractValidationException("Validation failed", new Dictionary<string, string[]>());
        var validationStateChangedNotifications = 0;

        editContext.OnValidationStateChanged += (_, _) => validationStateChangedNotifications++;

        // Act
        EditContextValidationHelper.ApplyValidationErrors(editContext, serverValidationException);

        // Assert
        (editContext.GetValidationMessages()).ShouldBeEmpty();
        (validationStateChangedNotifications).ShouldBe(0);
    }

    [Fact]
    public void ApplyValidationErrors_when_one_field_has_multiple_errors_associates_all_messages_with_that_field()
    {
        // Arrange
        var model = new TestFormModel();
        var editContext = new EditContext(model);
        var serverValidationException = new ContractValidationException("Validation failed", new Dictionary<string, string[]>
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
        (emailMessages).ShouldBe(["Email is required.", "Email is invalid."]);
        (validationStateChangedNotifications).ShouldBe(1);
    }

    [Fact]
    public void ApplyValidationErrors_when_multiple_fields_have_errors_associates_each_message_with_its_field()
    {
        // Arrange
        var model = new TestFormModel();
        var editContext = new EditContext(model);
        var serverValidationException = new ContractValidationException("Validation failed", new Dictionary<string, string[]>
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

        (emailMessages).ShouldBe(["Email is invalid."]);
        (firstNameMessages).ShouldBe(["First name is required."]);
        (validationStateChangedNotifications).ShouldBe(1);
    }

    [Fact]
    public void ApplyValidationErrors_replaces_previous_server_validation_messages()
    {
        // Arrange
        var model = new TestFormModel();
        var editContext = new EditContext(model);
        var firstException = new ContractValidationException("Validation failed", new Dictionary<string, string[]>
        {
            [nameof(TestFormModel.Email)] = ["Email is invalid."]
        });
        var secondException = new ContractValidationException("Validation failed", new Dictionary<string, string[]>
        {
            [nameof(TestFormModel.FirstName)] = ["First name is required."]
        });
        var emailField = editContext.Field(nameof(TestFormModel.Email));
        var firstNameField = editContext.Field(nameof(TestFormModel.FirstName));

        // Act
        EditContextValidationHelper.ApplyValidationErrors(editContext, firstException);
        EditContextValidationHelper.ApplyValidationErrors(editContext, secondException);

        // Assert
        editContext.GetValidationMessages(emailField).ShouldBeEmpty();
        editContext.GetValidationMessages(firstNameField).ShouldBe(["First name is required."]);
    }

}
