using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SharedKernel.Configuration.Tests;

public sealed class OptionsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddValidatedOptions_registers_options_and_validator()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Test:Value"] = "from-configuration"
            })
            .Build();

        // Act
        services.AddValidatedOptions<TestOptions, TestOptionsValidator>(configuration.GetSection("Test"));

        // Assert
        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<TestOptions>>();
        var validators = serviceProvider.GetServices<IValidateOptions<TestOptions>>();
        Assert.Equal("from-configuration", options.Value.Value);
        Assert.Contains(validators, validator => validator is TestOptionsValidator);
    }

    [Fact]
    public void AddValidatedOptions_rejects_null_services()
    {
        // Arrange
        const string ExpectedParameterName = "services";

        // Act
        var exception = Assert.Throws<ArgumentNullException>(() =>
            OptionsServiceCollectionExtensions.AddValidatedOptions<TestOptions, TestOptionsValidator>(
                null,
                new ConfigurationBuilder().Build().GetSection("Test")));

        // Assert
        Assert.Equal(ExpectedParameterName, exception.ParamName);
    }

    [Fact]
    public void AddValidatedOptions_rejects_null_configuration()
    {
        // Arrange
        const string ExpectedParameterName = "configuration";
        var services = new ServiceCollection();

        // Act
        var exception = Assert.Throws<ArgumentNullException>(() =>
            services.AddValidatedOptions<TestOptions, TestOptionsValidator>(null));

        // Assert
        Assert.Equal(ExpectedParameterName, exception.ParamName);
    }
}
