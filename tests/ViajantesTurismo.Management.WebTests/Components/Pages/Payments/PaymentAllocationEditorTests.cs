using ViajantesTurismo.Management.Web.Components.Pages.Payments;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Payments;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
public sealed class PaymentAllocationEditorTests : BunitContext
{
    [Fact]
    public void Renders_one_labelled_row_per_allocation_with_the_target_choices()
    {
        // Arrange
        var first = new PaymentTargetChoice(Guid.NewGuid(), "First installment");
        var second = new PaymentTargetChoice(Guid.NewGuid(), "Second installment");
        List<PaymentAllocationFormRow> rows = [new() { TargetId = second.Id, EuroAmount = 50m }];

        // Act
        var cut = Render<PaymentAllocationEditor>(parameters => parameters
            .Add(editor => editor.Rows, rows)
            .Add(editor => editor.Choices, [first, second])
            .Add(editor => editor.Prefix, "receipt"));

        // Assert
        cut.Find("legend").TextContent.ShouldBe("Explicit EUR installment selections");
        cut.Find("label[for='receipt-target-0']").TextContent.ShouldBe("Target");
        cut.Find("label[for='receipt-amount-0']").TextContent.ShouldBe("EUR credit");
        var options = cut.FindAll("#receipt-target-0 option").Select(option => option.TextContent).ToList();
        options.ShouldBe(["Select target", "First installment", "Second installment"]);
        cut.Find("#receipt-target-0").GetAttribute("value").ShouldBe(second.Id.ToString());
        cut.Find("#receipt-amount-0").GetAttribute("value").ShouldBe("50");
        cut.Find("fieldset").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public async Task Adding_and_removing_rows_changes_the_edited_allocations()
    {
        // Arrange
        List<PaymentAllocationFormRow> rows = [];
        var cut = Render<PaymentAllocationEditor>(parameters => parameters
            .Add(editor => editor.Rows, rows)
            .Add(editor => editor.Choices, [])
            .Add(editor => editor.Prefix, "allocate")
            .Add(editor => editor.Legend, "Custom legend"));

        // Act
        await cut.Find("button.btn-outline-secondary:not([aria-label])").ClickAsync();
        await cut.Find("button.btn-outline-secondary:not([aria-label])").ClickAsync();
        await cut.Find("button[aria-label='Remove allocation row 1']").ClickAsync();

        // Assert
        cut.Find("legend").TextContent.ShouldBe("Custom legend");
        rows.Count.ShouldBe(1);
        cut.FindAll("button[aria-label^='Remove allocation row']").ShouldHaveSingleItem().GetAttribute("aria-label").ShouldBe("Remove allocation row 1");
    }

    [Fact]
    public async Task Choosing_a_target_and_an_amount_updates_the_edited_row()
    {
        // Arrange
        var choice = new PaymentTargetChoice(Guid.NewGuid(), "First installment");
        List<PaymentAllocationFormRow> rows = [new()];
        var cut = Render<PaymentAllocationEditor>(parameters => parameters
            .Add(editor => editor.Rows, rows)
            .Add(editor => editor.Choices, [choice])
            .Add(editor => editor.Prefix, "refund"));

        // Act
        await cut.Find("#refund-target-0").ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = choice.Id.ToString() });
        await cut.Find("#refund-amount-0").ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "12.5" });

        // Assert
        var row = rows.ShouldHaveSingleItem();
        row.TargetId.ShouldBe(choice.Id);
        row.EuroAmount.ShouldBe(12.5m);
    }

    [Fact]
    public void Disabled_editor_disables_its_whole_fieldset()
    {
        // Arrange
        List<PaymentAllocationFormRow> rows = [new()];

        // Act
        var cut = Render<PaymentAllocationEditor>(parameters => parameters
            .Add(editor => editor.Rows, rows)
            .Add(editor => editor.Choices, [])
            .Add(editor => editor.Prefix, "release")
            .Add(editor => editor.Disabled, true));

        // Assert
        cut.Find("fieldset").HasAttribute("disabled").ShouldBeTrue();
    }
}
