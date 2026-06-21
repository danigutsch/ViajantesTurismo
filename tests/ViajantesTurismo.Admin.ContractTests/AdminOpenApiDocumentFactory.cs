using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace ViajantesTurismo.Admin.ContractTests;

internal static class AdminOpenApiDocumentFactory
{
    public static async Task<OpenApiDocument> CreateDocument(
        string documentName,
        CancellationToken cancellationToken,
        params string[] endpointMapperNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentName);

        var assembly = Assembly.Load("ViajantesTurismo.Admin.ApiService");
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddEndpointsApiExplorer();

        InvokeInternalStaticMethod(
            assembly,
            "ViajantesTurismo.Admin.ApiService.AdminOpenApiDocuments",
            "AddAdminOpenApiDocuments",
            builder.Services);

        await using var app = builder.Build();

        foreach (var endpointMapperName in endpointMapperNames)
        {
            InvokeInternalStaticMethod(
                assembly,
                $"ViajantesTurismo.Admin.ApiService.{endpointMapperName[3..]}",
                endpointMapperName,
                app);
        }

        await app.StartAsync(cancellationToken);

        using var scope = app.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredKeyedService<IOpenApiDocumentProvider>(documentName);
        return await provider.GetOpenApiDocumentAsync(cancellationToken);
    }

    private static void InvokeInternalStaticMethod(
        Assembly assembly,
        string typeName,
        string methodName,
        object argument)
    {
        var type = assembly.GetType(typeName) ?? throw new InvalidOperationException($"Type '{typeName}' was not found.");
        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Method '{typeName}.{methodName}' was not found.");

        _ = method.Invoke(null, [argument]);
    }
}
