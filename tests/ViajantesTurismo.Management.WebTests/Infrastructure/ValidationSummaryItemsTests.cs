using ViajantesTurismo.Management.Web.Helpers;

namespace ViajantesTurismo.Management.WebTests.Infrastructure;

[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class ValidationSummaryItemsTests
{
    [Fact]
    public void Lists_each_field_message_in_field_order_with_the_control_id_as_target()
    {
        // Arrange
        var editContext = new EditContext(new TestFormModel());
        var store = new ValidationMessageStore(editContext);
        store.Add(editContext.Field(nameof(TestFormModel.FirstName)), "First name is required.");
        store.Add(editContext.Field(nameof(TestFormModel.Email)), "Email is required.");
        store.Add(editContext.Field(nameof(TestFormModel.Email)), "Email is invalid.");
        KeyValuePair<string, string>[] fields =
        [
            new(nameof(TestFormModel.Email), "email-input"),
            new(nameof(TestFormModel.FirstName), "first-name-input")
        ];

        // Act
        var items = ValidationSummaryItems.From(editContext, fields);

        // Assert
        items.Select(item => (item.TargetId, item.Message)).ShouldBe(
        [
            ("email-input", "Email is required."),
            ("email-input", "Email is invalid."),
            ("first-name-input", "First name is required.")
        ]);
    }

    [Fact]
    public void Returns_a_new_list_for_every_call_so_the_summary_takes_focus_again()
    {
        // Arrange
        var editContext = new EditContext(new TestFormModel());
        new ValidationMessageStore(editContext).Add(editContext.Field(nameof(TestFormModel.Email)), "Email is invalid.");
        KeyValuePair<string, string>[] fields = [new(nameof(TestFormModel.Email), "email-input")];

        // Act
        var first = ValidationSummaryItems.From(editContext, fields);
        var second = ValidationSummaryItems.From(editContext, fields);

        // Assert
        second.ShouldNotBeSameAs(first);
        second.ShouldBe(first);
    }

    [Fact]
    public void Ignores_messages_for_fields_that_are_not_listed()
    {
        // Arrange
        var editContext = new EditContext(new TestFormModel());
        new ValidationMessageStore(editContext).Add(editContext.Field(nameof(TestFormModel.FirstName)), "First name is required.");
        KeyValuePair<string, string>[] fields = [new(nameof(TestFormModel.Email), "email-input")];

        // Act
        var items = ValidationSummaryItems.From(editContext, fields);

        // Assert
        items.ShouldBeEmpty();
    }
}
