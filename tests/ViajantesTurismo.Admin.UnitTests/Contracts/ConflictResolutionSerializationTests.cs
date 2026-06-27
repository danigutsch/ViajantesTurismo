using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.UnitTests.Contracts;

public class ConflictResolutionSerializationTests
{
    [Fact]
    public void Serialize_with_empty_conflict_resolutions_should_return_empty_string()
    {
        // Arrange
        IReadOnlyDictionary<string, string> conflictResolutions = new Dictionary<string, string>();

        // Act
        var serialized = ConflictResolutionSerialization.Serialize(conflictResolutions);

        // Assert
        Assert.Equal(string.Empty, serialized);
    }

    [Fact]
    public void Serialize_and_parse_with_encoded_values_should_round_trip_conflict_resolutions()
    {
        // Arrange
        IReadOnlyDictionary<string, string> conflictResolutions = new Dictionary<string, string>
        {
            ["qa+one@example.com"] = "merge & keep",
            ["qa.two@example.com"] = "replace/overwrite"
        };

        // Act
        var serialized = ConflictResolutionSerialization.Serialize(conflictResolutions);
        var parsed = ConflictResolutionSerialization.Parse(serialized);

        // Assert
        Assert.Equal("merge & keep", parsed["qa+one@example.com"]);
        Assert.Equal("replace/overwrite", parsed["qa.two@example.com"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_with_null_or_whitespace_input_should_return_empty_case_insensitive_dictionary(string? serialized)
    {
        // Arrange
        // Act
        var parsed = ConflictResolutionSerialization.Parse(serialized);

        // Assert
        Assert.Empty(parsed);
        Assert.Equal(StringComparer.OrdinalIgnoreCase, parsed.Comparer);
    }

    [Fact]
    public void Parse_with_malformed_pairs_should_ignore_invalid_entries()
    {
        // Arrange
        const string serialized = "missing-separator;=missing-email;missing-value=;valid%40example.com=merge";

        // Act
        var parsed = ConflictResolutionSerialization.Parse(serialized);

        // Assert
        Assert.Single(parsed);
        Assert.Equal("merge", parsed["valid@example.com"]);
    }

    [Fact]
    public void Parse_with_whitespace_email_or_decision_should_ignore_whitespace_only_entries()
    {
        // Arrange
        const string serialized = "%20=merge;blank-decision%40example.com=%20;valid%40example.com=keep";

        // Act
        var parsed = ConflictResolutionSerialization.Parse(serialized);

        // Assert
        Assert.Single(parsed);
        Assert.Equal("keep", parsed["valid@example.com"]);
    }

    [Fact]
    public void Parse_with_duplicate_email_different_casing_should_keep_last_value_in_case_insensitive_dictionary()
    {
        // Arrange
        const string serialized = "User%40Example.com=merge;user%40example.com=replace";

        // Act
        var parsed = ConflictResolutionSerialization.Parse(serialized);

        // Assert
        Assert.Single(parsed);
        Assert.Equal("replace", parsed["USER@example.com"]);
    }
}
