using Microsoft.Extensions.Configuration;

namespace SharedKernel.Configuration.Tests;

public sealed class OptionsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddValidatedOptions_registers_options_and_validator()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Test:Value"] = "from-configuration"
            })
            .Build();

        // Act
        var registration = TestOptionsServices.GetRegistration(configuration);

        // Assert
        Assert.Equal("from-configuration", registration.Options.Value.Value);
        Assert.Same(registration.Options.Value, registration.OptionsValue);
        Assert.Contains(registration.Validators, validator => validator is TestOptionsValidator);
    }

    [Fact]
    public void AddValidatedOptions_rejects_null_services()
    {
        // Arrange
        const string ExpectedParameterName = "services";

        // Act
        var exception = Assert.Throws<ArgumentNullException>(() =>
            OptionsServiceCollectionExtensions.AddValidatedOptions<TestOptions, TestOptionsValidator>(null));

        // Assert
        Assert.Equal(ExpectedParameterName, exception.ParamName);
    }
}
