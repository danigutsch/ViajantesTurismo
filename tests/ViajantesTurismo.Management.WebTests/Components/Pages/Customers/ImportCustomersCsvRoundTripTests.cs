using PandaBear.Testing;
using System.Text;
using ViajantesTurismo.Management.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

/// <summary>Verifies that the customer import reads RFC 4180 CSV and that the files it writes read back unchanged.</summary>
[Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[TestScope(TestScope.Unit)]
public sealed class ImportCustomersCsvRoundTripTests
{
    private const string QuotedFile =
        "\"Last, Name\",Email,Notes\r\n\"O'Neil, Jr.\",ada@example.com,\"Said \"\"hi\"\"\nthen left\"\r\n Plain ,bob@example.com,+351 912 000 000\r\n";

    [Fact]
    public void ReadHeaders_reads_quoted_headers_with_commas()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes(QuotedFile);

        // Act
        var headers = ImportCustomersCsvProcessor.ReadHeaders(bytes);

        // Assert
        headers.ShouldBe(["Last, Name", "Email", "Notes"]);
    }

    [Fact]
    public void ReadHeaders_reads_the_header_of_a_file_whose_rows_are_malformed()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("FirstName,Email\nonly-one-column");

        // Act
        var headers = ImportCustomersCsvProcessor.ReadHeaders(bytes);

        // Assert
        headers.ShouldBe(["FirstName", "Email"]);
    }

    [Fact]
    public void BuildPreviewRows_reads_quoted_values_with_commas_quotes_and_line_breaks()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes(QuotedFile);
        var mappings = new[]
        {
            new CustomerImportFieldMapping(new CustomerImportField("LastName", "Last name", true), "Last, Name"),
            new CustomerImportFieldMapping(new CustomerImportField("Notes", "Notes", false), "Notes"),
        };

        // Act
        var rows = ImportCustomersCsvProcessor.BuildPreviewRows(bytes, ["Last, Name", "Email", "Notes"], mappings, new Dictionary<string, string?>(), 5);

        // Assert
        rows.Count.ShouldBe(2);
        rows[0]["LastName"].ShouldBe("O'Neil, Jr.");
        rows[0]["Notes"].ShouldBe("Said \"hi\"\nthen left");
        rows[1]["LastName"].ShouldBe("Plain");
    }

    [Fact]
    public void Mixed_selections_write_a_file_that_reads_back_with_the_same_values()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes(QuotedFile);
        var state = new ImportCustomerConflictState(
            "ada@example.com",
            null,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Notes"] = "=kept, \"as text\"" });
        state.SetDecision("mixed", [new CustomerImportField("Notes", "Notes", false)]);
        state.SetFieldSource("Notes", ImportConflictFieldSource.Existing);

        // Act
        var written = ImportCustomersCsvProcessor.ApplyMixedFieldSelections(bytes, [state]);
        var rows = ImportCustomersCsvProcessor.ParseMappedRowsByEmail(written);

        // Assert
        rows["ada@example.com"]["Last, Name"].ShouldBe("O'Neil, Jr.");
        rows["ada@example.com"]["Notes"].ShouldBe("=kept, \"as text\"");
        rows["bob@example.com"]["Last, Name"].ShouldBe("Plain");
        rows["bob@example.com"]["Notes"].ShouldBe("+351 912 000 000");
    }

    [Fact]
    public void Mapping_headers_writes_a_file_that_reads_back_with_the_same_values()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes(QuotedFile);
        var mappings = CustomerImportHeaderMatcher.AutoMatch(["Last, Name", "Email", "Notes"]);
        var userMappings = new Dictionary<string, string?> { ["LastName"] = "Last, Name" };

        // Act
        var written = CustomerImportHeaderMatcher.ApplyMapping(bytes, mappings, userMappings);
        var rows = ImportCustomersCsvProcessor.ParseMappedRowsByEmail(written);

        // Assert
        ImportCustomersCsvProcessor.ReadHeaders(written).ShouldBe(["LastName", "Email", "Notes"]);
        rows["ada@example.com"]["LastName"].ShouldBe("O'Neil, Jr.");
        rows["ada@example.com"]["Notes"].ShouldBe("Said \"hi\"\nthen left");
        rows["bob@example.com"]["Notes"].ShouldBe("+351 912 000 000");
    }
}
