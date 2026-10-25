namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Reads and slices the hosted-test fixture source for the text-level pins of its on-demand design.</summary>
internal static class HostedTestFixtureSource
{
    public static string Read()
    {
        var path = Path.Combine(
            AdminTestArchitectureGuardTestsHelpers.GetRepositoryRoot(),
            "tests",
            "ViajantesTurismo.HostedTests",
            "Infrastructure",
            "HostedTestFixture.cs");

        return File.ReadAllText(path).ReplaceLineEndings("\n");
    }

    /// <summary>Reads the fixture with every whitespace run collapsed, so a statement can be pinned across line breaks.</summary>
    public static string ReadNormalized() => string.Join(' ', Read().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    public static int Count(string source, string token) => source.Split(token, StringSplitOptions.None).Length - 1;

    /// <summary>Returns the braced body of the member whose declaration starts with <paramref name="signature"/>.</summary>
    public static string Body(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"HostedTestFixture.cs does not declare '{signature}'.");
        }

        var open = source.IndexOf('{', start);
        if (open < 0)
        {
            throw new InvalidOperationException($"'{signature}' in HostedTestFixture.cs has no braced body.");
        }

        var depth = 0;
        for (var index = open; index < source.Length; index++)
        {
            depth += source[index] switch
            {
                '{' => 1,
                '}' => -1,
                _ => 0
            };

            if (depth == 0)
            {
                return source[open..(index + 1)];
            }
        }

        throw new InvalidOperationException($"The body of '{signature}' in HostedTestFixture.cs is not closed.");
    }
}
