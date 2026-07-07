using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.AI;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal static class CatalogAiTextGenerationDependencyInjection
{
    internal static TApplicationBuilder AddCatalogAiTextGeneration<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        var options = CreateOptions(builder.Configuration);

        builder.Services.AddSingleton(options);
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
