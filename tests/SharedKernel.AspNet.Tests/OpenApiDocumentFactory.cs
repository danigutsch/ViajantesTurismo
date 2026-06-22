using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using SharedKernel.OpenApi;

namespace SharedKernel.AspNet.Tests;

internal static class OpenApiDocumentFactory
{
    public static async Task<OpenApiDocument> CreateDocument(
        string documentName,
        Action<RouteGroupBuilder> configureGroup,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentName);
        ArgumentNullException.ThrowIfNull(configureGroup);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddBoundaryOpenApiDocuments([documentName]);

        await using var app = builder.Build();
        configureGroup(app.MapGroup($"/{documentName}").WithGroupName(documentName).WithTags(documentName));

        await app.StartAsync(ct);

        using var scope = app.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredKeyedService<IOpenApiDocumentProvider>(documentName);
        return await provider.GetOpenApiDocumentAsync(ct);
    }
}
