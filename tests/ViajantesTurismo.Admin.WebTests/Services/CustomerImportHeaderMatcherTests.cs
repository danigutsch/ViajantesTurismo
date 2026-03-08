using System.Text;
using ViajantesTurismo.Admin.Web.Services;

namespace ViajantesTurismo.Admin.WebTests.Services;

public sealed class CustomerImportHeaderMatcherTests
{
    [Fact]
    public void AutoMatch_Returns_Exact_Match_For_Canonical_Header()
    {
        var result = CustomerImportHeaderMatcher.AutoMatch(["FirstName", "Email"]);

        Assert.Equal("FirstName", result.First(m => m.Field.Name == "FirstName").MatchedCsvHeader);
        Assert.Equal("Email", result.First(m => m.Field.Name == "Email").MatchedCsvHeader);
    }

    [Fact]
    public void AutoMatch_Is_CaseInsensitive()
    {
        var result = CustomerImportHeaderMatcher.AutoMatch(["firstname", "LASTNAME", "eMaIl"]);

        Assert.Equal("firstname", result.First(m => m.Field.Name == "FirstName").MatchedCsvHeader);
        Assert.Equal("LASTNAME", result.First(m => m.Field.Name == "LastName").MatchedCsvHeader);
        Assert.Equal("eMaIl", result.First(m => m.Field.Name == "Email").MatchedCsvHeader);
    }

    [Fact]
    public void AutoMatch_Returns_Null_For_Unrecognized_Headers()
    {
        var result = CustomerImportHeaderMatcher.AutoMatch(["UnknownCol", "AnotherUnknown"]);

        Assert.All(result, m => Assert.Null(m.MatchedCsvHeader));
    }

    [Fact]
    public void AutoMatch_Returns_All_Known_Fields()
    {
        var result = CustomerImportHeaderMatcher.AutoMatch([]);

        Assert.Equal(CustomerImportHeaderMatcher.Fields.Count, result.Count);
    }

    [Fact]
    public void AutoMatch_Returns_Null_Match_For_Unmatched_Fields()
    {
        var result = CustomerImportHeaderMatcher.AutoMatch(["FirstName"]);

        var unmatched = result.Where(m => m.Field.Name != "FirstName");
        Assert.All(unmatched, m => Assert.False(m.IsAutoMatched));
    }

    [Fact]
    public void Fields_Email_Is_Required()
    {
        var field = CustomerImportHeaderMatcher.Fields.First(f => f.Name == "Email");

        Assert.True(field.IsRequired);
    }

    [Fact]
    public void Fields_Instagram_Is_Optional()
    {
        var field = CustomerImportHeaderMatcher.Fields.First(f => f.Name == "Instagram");

        Assert.False(field.IsRequired);
    }

    [Fact]
    public void Fields_Allergies_Is_Optional()
    {
        var field = CustomerImportHeaderMatcher.Fields.First(f => f.Name == "Allergies");

        Assert.False(field.IsRequired);
    }

    [Fact]
    public void ApplyMapping_Renames_CaseInsensitive_Match_To_Canonical_Form()
    {
        var bytes = Encoding.UTF8.GetBytes("firstname,lastname\njohn,doe\n");
        var mappings = CustomerImportHeaderMatcher.AutoMatch(["firstname", "lastname"]);

        var result = CustomerImportHeaderMatcher.ApplyMapping(bytes, mappings, new Dictionary<string, string?>());

        var text = Encoding.UTF8.GetString(result);
        Assert.StartsWith("FirstName,LastName", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyMapping_Applies_User_Assignment_When_No_AutoMatch()
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
    public void ApplyMapping_Keeps_Unrecognized_Extra_Columns_As_Is()
    {
        var bytes = Encoding.UTF8.GetBytes("FirstName,SomeExtra,Email\n");
        var mappings = CustomerImportHeaderMatcher.AutoMatch(["FirstName", "SomeExtra", "Email"]);

        var result = CustomerImportHeaderMatcher.ApplyMapping(bytes, mappings, new Dictionary<string, string?>());

        var text = Encoding.UTF8.GetString(result);
        Assert.StartsWith("FirstName,SomeExtra,Email", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyMapping_Preserves_Data_Rows()
    {
        var bytes = Encoding.UTF8.GetBytes("FirstName,LastName\njohn,doe\njane,smith\n");
        var mappings = CustomerImportHeaderMatcher.AutoMatch(["FirstName", "LastName"]);

        var result = CustomerImportHeaderMatcher.ApplyMapping(bytes, mappings, new Dictionary<string, string?>());

        var text = Encoding.UTF8.GetString(result);
        Assert.Contains("john,doe", text, StringComparison.Ordinal);
        Assert.Contains("jane,smith", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyMapping_File_With_No_Newline_Only_Remaps_Headers()
    {
        var bytes = Encoding.UTF8.GetBytes("firstname");
        var mappings = CustomerImportHeaderMatcher.AutoMatch(["firstname"]);

        var result = CustomerImportHeaderMatcher.ApplyMapping(bytes, mappings, new Dictionary<string, string?>());

        var text = Encoding.UTF8.GetString(result);
        Assert.StartsWith("FirstName", text, StringComparison.Ordinal);
    }
}
