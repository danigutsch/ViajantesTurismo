namespace SharedKernel.Configuration;

/// <summary>
/// Defines the configuration section used to bind an options type.
/// </summary>
public interface IConfigurationSectionOptions
{
    /// <summary>
    /// Gets the configuration section name used to bind the options type.
    /// </summary>
    static abstract string SectionName { get; }
}
