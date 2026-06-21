using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Xunit;

namespace ViajantesTurismo.Admin.ContractTests;

/// <summary>
/// Verifies that Admin named OpenAPI documents expose the intended boundary slices.
/// </summary>
public sealed class AdminOpenApiDocumentRegistrationTests
{
    [Fact]
    public async Task Generates_A_Tours_Document_Containing_Only_Tours_Paths()
    {
        var document = await CreateDocument(
            "tours",
            "MapToursEndpoints",
            "MapCustomerEndpoints",
            "MapBookingEndpoints");

        Assert.Contains("/tours", document.Paths.Keys);
        Assert.Contains("/tours/{id}", document.Paths.Keys);
        Assert.DoesNotContain("/customers", document.Paths.Keys);
        Assert.DoesNotContain("/bookings", document.Paths.Keys);
    }

    [Fact]
    public async Task Generates_A_Customers_Document_Including_Import_Paths()
    {
        var document = await CreateDocument(
            "customers",
            "MapCustomerEndpoints",
            "MapCustomerImportEndpoints",
            "MapToursEndpoints");

        Assert.Contains("/customers", document.Paths.Keys);
        Assert.Contains("/customers/{id}", document.Paths.Keys);
        Assert.Contains("/customers/import", document.Paths.Keys);
        Assert.Contains("/customers/import/commit", document.Paths.Keys);
        Assert.DoesNotContain("/tours", document.Paths.Keys);

        var importSchema = GetMultipartSchema(document, "/customers/import/commit");
        Assert.NotNull(importSchema.AllOf);
        Assert.Contains(importSchema.AllOf, static item => item.Properties?.ContainsKey("file") == true);
        Assert.Contains(importSchema.AllOf, static item => item.Properties?.ContainsKey("conflictResolutions") == true);
    }

    [Fact]
    public async Task Generates_A_V1_Document_Including_Error_Documentation_Paths()
    {
        var document = await CreateDocument(
            "v1",
            "MapToursEndpoints",
            "MapCustomerEndpoints",
            "MapCustomerImportEndpoints",
            "MapBookingEndpoints",
            "MapErrorDocumentationEndpoints");

        Assert.Contains("/docs/errors", document.Paths.Keys);
        Assert.Contains("/docs/errors/{identifier}", document.Paths.Keys);
    }

    private static async Task<OpenApiDocument> CreateDocument(string documentName, params string[] endpointMapperNames)
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

        await app.StartAsync(TestContext.Current.CancellationToken);

        using var scope = app.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredKeyedService<IOpenApiDocumentProvider>(documentName);
        return await provider.GetOpenApiDocumentAsync(TestContext.Current.CancellationToken);
    }

    private static void InvokeInternalStaticMethod(Assembly assembly, string typeName, string methodName, object argument)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        ArgumentNullException.ThrowIfNull(argument);

        var type = assembly.GetType(typeName) ?? throw new InvalidOperationException($"Type '{typeName}' was not found.");
        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Method '{typeName}.{methodName}' was not found.");

        _ = method.Invoke(null, [argument]);
    }

    private static OpenApiSchema GetMultipartSchema(OpenApiDocument document, string path)
    {
        if (!document.Paths.TryGetValue(path, out var pathItem) || pathItem.Operations is null)
        {
            throw new InvalidOperationException($"Expected OpenAPI path '{path}' to exist.");
        }

        if (!pathItem.Operations.TryGetValue(HttpMethod.Post, out var operation))
        {
            throw new InvalidOperationException($"Expected POST operation for '{path}'.");
        }

        return operation.RequestBody?.Content?["multipart/form-data"].Schema as OpenApiSchema
            ?? throw new InvalidOperationException($"Expected multipart schema for '{path}'.");
    }
}
