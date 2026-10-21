using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PandaBear.AI;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal static class CatalogAiTextGenerationDependencyInjection
{
    internal static TApplicationBuilder AddCatalogAiTextGeneration<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        var options = CreateOptions(builder.Configuration);

        builder.Services.AddSingleton(options);
        var adapterAssembly = typeof(LiteLlmImageTextGenerator).Assembly;
        var promptImplementationVersion = adapterAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? adapterAssembly.GetName().Version?.ToString()
            ?? "unreported";
        var nonSecretConfiguration = string.Join("\n", options.Model, options.ChatCompletionsPath, options.Endpoint?.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped));
        var configurationFingerprint = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(nonSecretConfiguration)));
        builder.Services.AddSingleton(new MediaImageAccessibilityGenerationProvenance("litellm", options.Model ?? "unconfigured", promptImplementationVersion, configurationFingerprint));
        builder.Services.AddHttpClient<IImageTextGenerator, LiteLlmImageTextGenerator>((serviceProvider, client) =>
        {
            var configuredOptions = serviceProvider.GetRequiredService<LiteLlmImageTextGeneratorOptions>();
            if (configuredOptions.Endpoint is not null)
            {
                client.BaseAddress = configuredOptions.Endpoint;
            }
        });

        return builder;
    }

    private static LiteLlmImageTextGeneratorOptions CreateOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(LiteLlmImageTextGeneratorOptions.SectionName);
        var endpointValue = section[nameof(LiteLlmImageTextGeneratorOptions.Endpoint)];
        Uri? endpoint = null;
        if (!string.IsNullOrWhiteSpace(endpointValue))
        {
            Uri.TryCreate(endpointValue, UriKind.Absolute, out endpoint);
        }

        return new LiteLlmImageTextGeneratorOptions
        {
            Endpoint = endpoint,
            ApiKey = section[nameof(LiteLlmImageTextGeneratorOptions.ApiKey)],
            Model = section[nameof(LiteLlmImageTextGeneratorOptions.Model)],
            ChatCompletionsPath = section[nameof(LiteLlmImageTextGeneratorOptions.ChatCompletionsPath)] ?? "/v1/chat/completions"
        };
    }
}
