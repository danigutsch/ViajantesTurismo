using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Common.UnitTests.Sanitizers;

public class StringSanitizerTests
{
    [Fact]
    public void Sanitize_Returns_Null_When_Input_Is_Null()
    {
        // Arrange
        // Act
        var result = StringSanitizer.Sanitize(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Sanitize_Returns_Empty_String_When_Input_Is_Empty()
    {
        // Arrange
        // Act
        var result = StringSanitizer.Sanitize(string.Empty);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Sanitize_Trims_Leading_Whitespace()
    {
        // Arrange
        // Act
        var result = StringSanitizer.Sanitize("   Hello");

        // Assert
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void Sanitize_Trims_Trailing_Whitespace()
    {
        // Arrange
        // Act
        var result = StringSanitizer.Sanitize("Hello   ");

        // Assert
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void Sanitize_Trims_Leading_And_Trailing_Whitespace()
    {
        // Arrange
        // Act
        var result = StringSanitizer.Sanitize("   Hello   ");

        // Assert
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void Sanitize_Normalizes_Multiple_Spaces_To_Single_Space()
    {
        // Arrange
        // Act
        var result = StringSanitizer.Sanitize("Hello    World");

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void Sanitize_Normalizes_Multiple_Whitespace_Characters_To_Single_Space()
    {
        // Arrange
        // Act
        var result = StringSanitizer.Sanitize("Hello \t\n  World");

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void Sanitize_Removes_Control_Characters()
    {
        // Arrange
        // Act
        var result = StringSanitizer.Sanitize("Hello" + "\x00\x01\x02" + "World");

        // Assert
        Assert.Equal("HelloWorld", result);
    }

    [Fact]
    public void Sanitize_Removes_Null_Character()
    {
        // Arrange
        // Act
        var result = StringSanitizer.Sanitize("Hello" + "\0" + "World");

        // Assert
        Assert.Equal("HelloWorld", result);
    }

    [Fact]
    public void Sanitize_Removes_Bell_Character()
    {
        // Arrange
        // Act
        var result = StringSanitizer.Sanitize("Hello" + "\x07" + "World");

        // Assert
        Assert.Equal("HelloWorld", result);
    }

    [Fact]
    public void Sanitize_Removes_Delete_Character()
    {
        // Arrange
        // Act
        var result = StringSanitizer.Sanitize("Hello" + "\x7F" + "World");

        // Assert
        Assert.Equal("HelloWorld", result);
    }

    [Fact]
    public void Sanitize_Preserves_Tab_Character_But_Normalizes_To_Space()
    {
        // Arrange
        // Act
        var result = StringSanitizer.Sanitize("Hello\tWorld");

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void Sanitize_Preserves_Newline_Character_But_Normalizes_To_Space()
    {
        // Arrange
        // Act
        var result = StringSanitizer.Sanitize("Hello\nWorld");

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void Sanitize_Handles_Complex_Sanitization_With_Multiple_Issues()
    {
        // Arrange
        // Act
        var result = StringSanitizer.Sanitize("  Hello" + "\x00" + "\t\t  World" + "\x01" + "  ");

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void Sanitize_Preserves_Valid_String_Without_Changes()
    {
        // Arrange
        var input = "Hello World";

        // Act
        var result = StringSanitizer.Sanitize(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void Sanitize_Handles_String_With_Only_Whitespace()
    {
        // Arrange
        // Act
        var result = StringSanitizer.Sanitize("   \t\n   ");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Sanitize_Handles_String_With_Special_Characters()
    {
        // Arrange
        var input = "Hello@World!#$%";

        // Act
        var result = StringSanitizer.Sanitize(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void Sanitize_Handles_String_With_Numbers()
    {
        // Arrange
        var input = "Test123";

        // Act
        var result = StringSanitizer.Sanitize(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void Sanitize_Handles_String_With_Accented_Characters()
    {
        // Arrange
        var input = "José García";

        // Act
        var result = StringSanitizer.Sanitize(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void Sanitize_Handles_String_With_Emoji()
    {
        // Arrange
        var input = "Hello 🌍 World";

        // Act
        var result = StringSanitizer.Sanitize(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void Sanitize_Notes_Returns_Null_When_Input_Is_Null()
    {
        // Arrange
        // Act
        var result = StringSanitizer.SanitizeNotes(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Sanitize_Notes_Returns_Empty_String_When_Input_Is_Empty()
    {
        // Arrange
        // Act
        var result = StringSanitizer.SanitizeNotes(string.Empty);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Sanitize_Notes_Trims_Leading_And_Trailing_Whitespace()
    {
        // Arrange
        // Act
        var result = StringSanitizer.SanitizeNotes("   Hello World   ");

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void Sanitize_Notes_Preserves_Newlines_Within_Text()
    {
        // Arrange
        var input = "Line 1\nLine 2\nLine 3";

        // Act
        var result = StringSanitizer.SanitizeNotes(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void Sanitize_Notes_Preserves_Multiple_Newlines()
    {
        // Arrange
        var input = "Paragraph 1\n\nParagraph 2";

        // Act
        var result = StringSanitizer.SanitizeNotes(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void Sanitize_Notes_Preserves_Tabs_Within_Text()
    {
        // Arrange
        var input = "Column1\tColumn2\tColumn3";

        // Act
        var result = StringSanitizer.SanitizeNotes(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void Sanitize_Notes_Preserves_Multiple_Spaces()
    {
        // Arrange
        var input = "Hello    World";

        // Act
        var result = StringSanitizer.SanitizeNotes(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void Sanitize_Notes_Preserves_Formatting_In_Multi_Line_Notes()
    {
        // Arrange
        var input = "Note:\n  - Item 1\n  - Item 2\n  - Item 3";

        // Act
        var result = StringSanitizer.SanitizeNotes(input);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void Sanitize_Notes_Handles_String_With_Only_Whitespace()
    {
        // Arrange
        // Act
        var result = StringSanitizer.SanitizeNotes("   \t\n   ");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Sanitize_Collection_Returns_Empty_Array_When_Input_Is_Null()
    {
        // Arrange
        // Act
        var result = StringSanitizer.SanitizeCollection(null);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Sanitize_Collection_Returns_Empty_Array_When_Input_Is_Empty()
    {
        // Arrange
        // Act
        var result = StringSanitizer.SanitizeCollection([]);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Sanitize_Collection_Removes_Null_Entries()
    {
        // Arrange
        var input = new[] { "Hello", null, "World" };

        // Act
        var result = StringSanitizer.SanitizeCollection(input!);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Contains("Hello", result);
        Assert.Contains("World", result);
    }

    [Fact]
    public void Sanitize_Collection_Removes_Empty_String_Entries()
    {
        // Arrange
        var input = new[] { "Hello", "", "World" };

        // Act
        var result = StringSanitizer.SanitizeCollection(input);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Contains("Hello", result);
        Assert.Contains("World", result);
    }

    [Fact]
    public void Sanitize_Collection_Removes_Whitespace_Only_Entries()
    {
        // Arrange
        var input = new[] { "Hello", "   ", "World" };

        // Act
        var result = StringSanitizer.SanitizeCollection(input);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Contains("Hello", result);
        Assert.Contains("World", result);
    }

    [Fact]
    public void Sanitize_Collection_Trims_Each_Entry()
    {
        // Arrange
        var input = new[] { "  Hello  ", "  World  " };

        // Act
        var result = StringSanitizer.SanitizeCollection(input);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Contains("Hello", result);
        Assert.Contains("World", result);
    }

    [Fact]
    public void Sanitize_Collection_Removes_Duplicate_Entries_Case_Insensitive()
    {
        // Arrange
        var input = new[] { "Hello", "hello", "HELLO", "World" };

        // Act
        var result = StringSanitizer.SanitizeCollection(input);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Contains("Hello", result);
        Assert.Contains("World", result);
    }

    [Fact]
    public void Sanitize_Collection_Preserves_First_Occurrence_Of_Duplicate()
    {
        // Arrange
        var input = new[] { "Hello", "hello" };

        // Act
        var result = StringSanitizer.SanitizeCollection(input);

        // Assert
        Assert.Single(result);
        Assert.Equal("Hello", result[0]);
    }

    [Fact]
    public void Sanitize_Collection_Sanitizes_Each_Entry()
    {
        // Arrange
        var input = new[] { "  Hello" + "\x00" + "  ", "  World\t\t  " };

        // Act
        var result = StringSanitizer.SanitizeCollection(input);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Contains("Hello", result);
        Assert.Contains("World", result);
    }

    [Fact]
    public void Sanitize_Collection_Handles_Complex_Collection_With_Multiple_Issues()
    {
        // Arrange
        var input = new[] { "  Item1  ", null, "", "item1", "  Item2  ", "   ", "Item3" };

        // Act
        var result = StringSanitizer.SanitizeCollection(input!);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Contains("Item1", result);
        Assert.Contains("Item2", result);
        Assert.Contains("Item3", result);
    }

    [Fact]
    public void Sanitize_Collection_Returns_Empty_Array_When_All_Entries_Are_Invalid()
    {
        // Arrange
        var input = new[] { null, "", "   ", "\t\n" };

        // Act
        var result = StringSanitizer.SanitizeCollection(input!);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Sanitize_Collection_Preserves_Order_Of_First_Occurrences()
    {
        // Arrange
        var input = new[] { "Zebra", "Apple", "Banana" };

        // Act
        var result = StringSanitizer.SanitizeCollection(input);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("Zebra", result[0]);
        Assert.Equal("Apple", result[1]);
        Assert.Equal("Banana", result[2]);
    }

    [Fact]
    public void Sanitize_Collection_Handles_Collection_With_Special_Characters()
    {
        // Arrange
        var input = new[] { "Item@1", "Item#2", "Item$3" };

        // Act
        var result = StringSanitizer.SanitizeCollection(input);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Contains("Item@1", result);
        Assert.Contains("Item#2", result);
        Assert.Contains("Item$3", result);
    }

    [Fact]
    public void Sanitize_Collection_Handles_Collection_With_Unicode_Characters()
    {
        // Arrange
        var input = new[] { "José", "José", "María" };

        // Act
        var result = StringSanitizer.SanitizeCollection(input);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Contains("José", result);
        Assert.Contains("María", result);
    }
}
