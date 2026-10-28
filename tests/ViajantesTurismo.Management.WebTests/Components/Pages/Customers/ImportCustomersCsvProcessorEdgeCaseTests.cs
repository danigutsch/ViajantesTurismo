using System.Text;
using ViajantesTurismo.Management.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class ImportCustomersCsvProcessorEdgeCaseTests
{
    [Fact]
    public void BuildPreviewRows_when_the_file_has_only_a_header_returns_no_rows()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("Email,FirstName");
        var mappings = new[] { new CustomerImportFieldMapping(new CustomerImportField("Email", "Email", true), "Email") };

        // Act
        var rows = ImportCustomersCsvProcessor.BuildPreviewRows(bytes, ["Email", "FirstName"], mappings, new Dictionary<string, string?>(), 5);

        // Assert
        rows.ShouldBeEmpty();
    }

    [Fact]
    public void BuildPreviewRows_when_a_field_is_mapped_to_no_column_leaves_that_field_out_of_the_preview()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("Email,Name\nada@example.com,Ada");
        var mappings = new[]
        {
            new CustomerImportFieldMapping(new CustomerImportField("Email", "Email", true), "Email"),
            new CustomerImportFieldMapping(new CustomerImportField("FirstName", "First name", false), "Name"),
        };
        var userMappings = new Dictionary<string, string?> { ["FirstName"] = null };

        // Act
        var rows = ImportCustomersCsvProcessor.BuildPreviewRows(bytes, ["Email", "Name"], mappings, userMappings, 5);

        // Assert
        var row = rows.ShouldHaveSingleItem();
        row["Email"].ShouldBe("ada@example.com");
        row.ContainsKey("FirstName").ShouldBeFalse();
    }

    [Fact]
    public void ParseMappedRowsByEmail_when_the_file_has_only_a_header_returns_no_rows()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("Email,FirstName");

        // Act
        var rows = ImportCustomersCsvProcessor.ParseMappedRowsByEmail(bytes);

        // Assert
        rows.ShouldBeEmpty();
    }

    [Fact]
    public void ParseMappedRowsByEmail_when_there_is_no_email_column_returns_no_rows()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("FirstName,LastName\nAda,Lovelace");

        // Act
        var rows = ImportCustomersCsvProcessor.ParseMappedRowsByEmail(bytes);

        // Assert
        rows.ShouldBeEmpty();
    }

    [Fact]
    public void ParseMappedRowsByEmail_skips_rows_that_are_too_short_or_have_a_blank_email()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("FirstName,Email\nShort\nBlank,  \nAda,ada@example.com");

        // Act
        var rows = ImportCustomersCsvProcessor.ParseMappedRowsByEmail(bytes);

        // Assert
        var row = rows.ShouldHaveSingleItem();
        row.Key.ShouldBe("ada@example.com");
        row.Value["FirstName"].ShouldBe("Ada");
    }

    [Fact]
    public void ApplyMixedFieldSelections_when_the_file_has_only_a_header_returns_the_same_bytes()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("Email,FirstName");

        // Act
        var result = ImportCustomersCsvProcessor.ApplyMixedFieldSelections(bytes, []);

        // Assert
        result.ShouldBeSameAs(bytes);
    }

    [Fact]
    public void ApplyMixedFieldSelections_when_there_is_no_email_column_returns_the_same_bytes()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("FirstName,LastName\nAda,Lovelace");

        // Act
        var result = ImportCustomersCsvProcessor.ApplyMixedFieldSelections(bytes, []);

        // Assert
        result.ShouldBeSameAs(bytes);
    }

    [Fact]
    public void ApplyMixedFieldSelections_keeps_rows_without_a_usable_mixed_decision_unchanged()
    {
        // Arrange
        var csv = "FirstName,Email\nshort\nKept,keep@example.com\nIncoming,none@example.com\nIncoming,empty@example.com";
        var bytes = Encoding.UTF8.GetBytes(csv);
        var keepState = new ImportCustomerConflictState("keep@example.com", null, null);
        keepState.SetDecision("keep", CustomerImportHeaderMatcher.Fields);
        var mixedWithoutSelections = new ImportCustomerConflictState("empty@example.com", null, null);
        mixedWithoutSelections.SetDecision("mixed", Array.Empty<CustomerImportField>());

        // Act
        var result = ImportCustomersCsvProcessor.ApplyMixedFieldSelections(bytes, [keepState, mixedWithoutSelections]);

        // Assert
        Encoding.UTF8.GetString(result).ShouldBe(csv);
    }

    [Fact]
    public void ApplyMixedFieldSelections_ignores_existing_choices_the_file_or_the_stored_customer_cannot_supply()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("Email,FirstName\nmixed@example.com,Incoming");
        var state = new ImportCustomerConflictState(
            "mixed@example.com",
            null,
            new Dictionary<string, string> { ["LastName"] = "Existing" });
        state.SetDecision("mixed", Array.Empty<CustomerImportField>());
        state.SetFieldSource("LastName", ImportConflictFieldSource.Existing);
        state.SetFieldSource("FirstName", ImportConflictFieldSource.Existing);
        state.SetFieldSource("Email", ImportConflictFieldSource.Incoming);

        // Act
        var result = ImportCustomersCsvProcessor.ApplyMixedFieldSelections(bytes, [state]);

        // Assert
        Encoding.UTF8.GetString(result).ShouldBe("Email,FirstName\nmixed@example.com,Incoming");
    }

    [Fact]
    public void BuildImportSummary_when_there_is_no_result_returns_zero_counts()
    {
        // Arrange
        var keepState = new ImportCustomerConflictState("keep@example.com", null, null);
        keepState.SetDecision("keep", CustomerImportHeaderMatcher.Fields);

        // Act
        var summary = ImportCustomersCsvProcessor.BuildImportSummary(null, [keepState]);

        // Assert
        summary.ShouldBe(new ImportCustomersSummaryCounts(0, 0, 0, 0));
    }
}
