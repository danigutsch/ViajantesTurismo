using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SharedKernel.Configuration.Tests;

internal static class TestOptionsServices
{
    public static TestOptionsRegistration GetRegistration(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        AddValidatedOptionsToServices(services);
        using var serviceProvider = services.BuildServiceProvider();
        return new TestOptionsRegistration(
            serviceProvider.GetRequiredService<IOptions<TestOptions>>(),
            serviceProvider.GetRequiredService<TestOptions>(),
            serviceProvider.GetServices<IValidateOptions<TestOptions>>().ToArray());
    }

    public static void AddValidatedOptionsToServices()
    {
        AddValidatedOptionsToServices(new ServiceCollection());
    }

    private static void AddValidatedOptionsToServices(IServiceCollection services)
    {
        services.AddValidatedOptions<TestOptions, TestOptionsValidator>();
    }
}
