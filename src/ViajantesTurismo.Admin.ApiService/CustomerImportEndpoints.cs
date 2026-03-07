using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.ApiService;

/// <summary>
/// Defines endpoints for bulk customer import operations.
/// </summary>
internal static class CustomerImportEndpoints
{
    /// <summary>
    /// Maps all customer import endpoints to the application.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    public static WebApplication MapCustomerImportEndpoints(this WebApplication app)
    {
        app.MapPost("/customers/import", ImportCustomers)
            .WithGroupName("Customers")
            .WithTags("Customers")
            .WithName("ImportCustomers")
            .WithDescription("Imports customers from a CSV file.")
            .WithSummary("Imports customers from a CSV file.")
            .DisableAntiforgery();

        return app;
    }

    private static async Task<Ok<ImportResultDto>> ImportCustomers(
        IFormFile file,
        [FromServices] CustomerImportCommandHandler handler,
        CancellationToken ct)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        var csvText = await reader.ReadToEndAsync(ct);

        var result = await handler.Handle(new CustomerImportCommand(csvText, false), ct);

        return TypedResults.Ok(new ImportResultDto(result.SuccessCount, result.ErrorCount));
    }
}
