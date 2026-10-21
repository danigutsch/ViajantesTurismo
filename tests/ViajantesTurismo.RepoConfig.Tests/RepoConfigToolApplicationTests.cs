namespace ViajantesTurismo.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class RepoConfigToolApplicationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("--help")]
    [InlineData("-h")]
    public async Task Help_lists_bootstrap_and_json_validation_commands(string? help)
    {
        // Arrange
        using var output = new StringWriter();
        using var error = new StringWriter();
        string[] arguments = help is null ? [] : [help];

        // Act
        var exitCode = await RepoConfigToolApplication.Run(
            arguments, output, error, Directory.GetCurrentDirectory(), TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(0);
        output.ToString().ShouldContain("bootstrap-sharedkernel", StringComparison.Ordinal);
        output.ToString().ShouldContain("lint-json", StringComparison.Ordinal);
        error.ToString().ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Unknown_commands_explain_the_reusable_repository_cli_without_claiming_success()
    {
        // Arrange
        using var output = new StringWriter();
        using var error = new StringWriter();

        // Act
        var exitCode = await RepoConfigToolApplication.Run(
            ["unknown-command"], output, error, Directory.GetCurrentDirectory(), TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(1);
        output.ToString().ShouldBe(string.Empty);
        error.ToString().ShouldContain("Commands: bootstrap-sharedkernel", StringComparison.Ordinal);
        error.ToString().ShouldContain("Use sharedkernel-repo for reusable repository commands.", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("São Paulo 日本語 😀", "São Paulo 日本語 😀")]
    [InlineData("before\0\t\n\r\u001B\u007Fafter", "before\\u0000\\u0009\\u000A\\u000D\\u001B\\u007Fafter")]
    [InlineData("before\u00AD\u202Eafter", "before\\u00AD\\u202Eafter")]
    [InlineData("before\u2028\u2029after", "before\\u2028\\u2029after")]
    [InlineData("before\U000E0001after", "before\\U000E0001after")]
    [InlineData("before\n😀after", "before\\u000A😀after")]
    public void Diagnostics_preserve_readable_unicode_and_escape_terminal_controls(string value, string expected)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(value);

        // Act
        var escaped = RepoConfigToolApplication.EscapeControlCharacters(value);

        // Assert
        escaped.ShouldBe(expected);
    }

    [Theory]
    [InlineData('\uD800', "\\uD800")]
    [InlineData('\uDC00', "\\uDC00")]
    public void Diagnostics_escape_lone_surrogates_without_losing_surrounding_text(char surrogate, string expected)
    {
        // Arrange
        var value = "before" + surrogate + "after";

        // Act
        var escaped = RepoConfigToolApplication.EscapeControlCharacters(value);

        // Assert
        escaped.ShouldBe("before" + expected + "after");
    }
}
