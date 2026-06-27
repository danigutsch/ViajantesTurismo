using System.Text;

namespace ViajantesTurismo.Management.WebTests.Services;

public sealed class CustomerImportHeaderMatcherTests
{
    [Fact]
    public void AutoMatch_returns_exact_match_for_canonical_header()
    {
        var result = CustomerImportHeaderMatcher.AutoMatch(["FirstName", "Email"]);

        Assert.Equal("FirstName", result.First(m => m.Field.Name == "FirstName").MatchedCsvHeader);
        Assert.Equal("Email", result.First(m => m.Field.Name == "Email").MatchedCsvHeader);
    }

    [Fact]
    public void AutoMatch_is_caseinsensitive()
    {
        var result = CustomerImportHeaderMatcher.AutoMatch(["firstname", "LASTNAME", "eMaIl"]);

        Assert.Equal("firstname", result.First(m => m.Field.Name == "FirstName").MatchedCsvHeader);
        Assert.Equal("LASTNAME", result.First(m => m.Field.Name == "LastName").MatchedCsvHeader);
        Assert.Equal("eMaIl", result.First(m => m.Field.Name == "Email").MatchedCsvHeader);
    }

    [Fact]
    public void AutoMatch_returns_null_for_unrecognized_headers()
    {
        var result = CustomerImportHeaderMatcher.AutoMatch(["UnknownCol", "AnotherUnknown"]);

        Assert.All(result, m => Assert.Null(m.MatchedCsvHeader));
    }

    [Fact]
    public void AutoMatch_returns_all_known_fields()
    {
        var result = CustomerImportHeaderMatcher.AutoMatch([]);

        Assert.Equal(CustomerImportHeaderMatcher.Fields.Count, result.Count);
    }

    [Fact]
    public void AutoMatch_returns_null_match_for_unmatched_fields()
    {
        var result = CustomerImportHeaderMatcher.AutoMatch(["FirstName"]);

        var unmatched = result.Where(m => m.Field.Name != "FirstName");
        Assert.All(unmatched, m => Assert.False(m.IsAutoMatched));
    }

    [Fact]
    public void Fields_email_is_required()
    {
        var field = CustomerImportHeaderMatcher.Fields.First(f => f.Name == "Email");

        Assert.True(field.IsRequired);
    }

    [Fact]
    public void Fields_instagram_is_optional()
    {
        var field = CustomerImportHeaderMatcher.Fields.First(f => f.Name == "Instagram");

        Assert.False(field.IsRequired);
    }

    [Fact]
    public void Fields_allergies_is_optional()
    {
        var field = CustomerImportHeaderMatcher.Fields.First(f => f.Name == "Allergies");

        Assert.False(field.IsRequired);
    }

    [Fact]
    public void ApplyMapping_renames_caseinsensitive_match_to_canonical_form()
    {
        var bytes = Encoding.UTF8.GetBytes("firstname,lastname\njohn,doe\n");
        var mappings = CustomerImportHeaderMatcher.AutoMatch(["firstname", "lastname"]);

        var result = CustomerImportHeaderMatcher.ApplyMapping(bytes, mappings, new Dictionary<string, string?>());

        var text = Encoding.UTF8.GetString(result);
        Assert.StartsWith("FirstName,LastName", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyMapping_applies_user_assignment_when_no_automatch()
    {
        var bytes = Encoding.UTF8.GetBytes("Full Name,email_col\nJohn Doe,john@test.com\n");
        var mappings = CustomerImportHeaderMatcher.AutoMatch(["Full Name", "email_col"]);
        var userMappings = new Dictionary<string, string?>
        {
            ["FirstName"] = "Full Name",
            ["Email"] = "email_col",
        };

        var result = CustomerImportHeaderMatcher.ApplyMapping(bytes, mappings, userMappings);

        var text = Encoding.UTF8.GetString(result);
        Assert.StartsWith("FirstName,Email", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyMapping_keeps_unrecognized_extra_columns_as_is()
    {
        var bytes = Encoding.UTF8.GetBytes("FirstName,SomeExtra,Email\n");
        var mappings = CustomerImportHeaderMatcher.AutoMatch(["FirstName", "SomeExtra", "Email"]);

        var result = CustomerImportHeaderMatcher.ApplyMapping(bytes, mappings, new Dictionary<string, string?>());

        var text = Encoding.UTF8.GetString(result);
        Assert.StartsWith("FirstName,SomeExtra,Email", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyMapping_preserves_data_rows()
    {
        var bytes = Encoding.UTF8.GetBytes("FirstName,LastName\njohn,doe\njane,smith\n");
        var mappings = CustomerImportHeaderMatcher.AutoMatch(["FirstName", "LastName"]);

        var result = CustomerImportHeaderMatcher.ApplyMapping(bytes, mappings, new Dictionary<string, string?>());

        var text = Encoding.UTF8.GetString(result);
        Assert.Contains("john,doe", text, StringComparison.Ordinal);
        Assert.Contains("jane,smith", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyMapping_file_with_no_newline_only_remaps_headers()
    {
        var bytes = Encoding.UTF8.GetBytes("firstname");
        var mappings = CustomerImportHeaderMatcher.AutoMatch(["firstname"]);

        var result = CustomerImportHeaderMatcher.ApplyMapping(bytes, mappings, new Dictionary<string, string?>());

        var text = Encoding.UTF8.GetString(result);
        Assert.StartsWith("FirstName", text, StringComparison.Ordinal);
    }
}
