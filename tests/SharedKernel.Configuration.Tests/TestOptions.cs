namespace SharedKernel.Configuration.Tests;

internal sealed class TestOptions : IConfigurationSectionOptions
{
    public static string SectionName => "Test";

    public string Value { get; set; } = "configured";
}
