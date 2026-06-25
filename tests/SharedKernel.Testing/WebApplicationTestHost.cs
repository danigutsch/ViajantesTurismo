namespace SharedKernel.Testing;

/// <summary>
/// Creates ASP.NET Core test hosts with optional environment and service overrides.
/// </summary>
public static class WebApplicationTestHost
{
    /// <summary>
    /// Creates a web application factory for the target application assembly.
    /// </summary>
    public static Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<TEntryPoint> Create<TEntryPoint>(
        string? environment = null,
        Action<Microsoft.Extensions.DependencyInjection.IServiceCollection>? configureTestServices = null)
        where TEntryPoint : class
    {
        return new ConfigurableWebApplicationFactory<TEntryPoint>(environment, configureTestServices);
    }

    private sealed class ConfigurableWebApplicationFactory<TEntryPoint>(
        string? environment,
        Action<Microsoft.Extensions.DependencyInjection.IServiceCollection>? configureTestServices)
        : Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<TEntryPoint>
        where TEntryPoint : class
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            if (environment is not null)
            {
                Microsoft.AspNetCore.Hosting.HostingAbstractionsWebHostBuilderExtensions.UseEnvironment(
                    builder,
                    environment);
            }

            if (configureTestServices is not null)
            {
                Microsoft.AspNetCore.TestHost.WebHostBuilderExtensions.ConfigureTestServices(
                    builder,
                    configureTestServices);
            }
        }
    }
}
