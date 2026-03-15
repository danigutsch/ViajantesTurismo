using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.UnitTests.Contracts;

public class ConflictResolutionSerializationTests
{
    [Fact]
    public void Serialize_And_Parse_With_Encoded_Values_Should_Round_Trip_Conflict_Resolutions()
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

    [Fact]
    public void Parse_With_Malformed_Pairs_Should_Ignore_Invalid_Entries()
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
    public void Parse_With_Duplicate_Email_Different_Casing_Should_Keep_Last_Value_In_Case_Insensitive_Dictionary()
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
