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

        app.MapPost("/customers/import/commit", CommitImportWithResolutions)
            .WithGroupName("Customers")
            .WithTags("Customers")
            .WithName("CommitImportWithResolutions")
            .WithDescription("Commits customer import applying conflict resolutions.")
            .WithSummary("Commits customer import applying conflict resolutions.")
            .DisableAntiforgery();

        return app;
    }

    private static async Task<Ok<ImportResultDto>> ImportCustomers(
        IFormFile file,
        [FromServices] CustomerImportWorkflowService workflow,
        CancellationToken ct)
    {
        var csvText = await ReadCsvAsync(file, ct);
        var result = await workflow.ImportAsync(csvText, ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<ImportResultDto>> CommitImportWithResolutions(
        IFormFile file,
        [FromForm(Name = "conflictResolutions")]
        string? conflictResolutions,
        [FromServices] CustomerImportWorkflowService workflow,
        CancellationToken ct)
    {
        var csvText = await ReadCsvAsync(file, ct);
        var parsedConflictResolutions = ParseConflictResolutions(conflictResolutions);
        var result = await workflow.CommitAsync(
            csvText,
            parsedConflictResolutions,
            ct);

        return TypedResults.Ok(result);
    }

    private static async Task<string> ReadCsvAsync(IFormFile file, CancellationToken ct)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        return await reader.ReadToEndAsync(ct);
    }

    private static Dictionary<string, string> ParseConflictResolutions(string? serializedConflictResolutions)
    {
        if (string.IsNullOrWhiteSpace(serializedConflictResolutions))
        {
            return new(StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in serializedConflictResolutions.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = pair.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0 || separatorIndex == pair.Length - 1)
            {
                continue;
            }

            var email = Uri.UnescapeDataString(pair[..separatorIndex]);
            var decision = Uri.UnescapeDataString(pair[(separatorIndex + 1)..]);

            if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(decision))
            {
                result[email] = decision;
            }
        }

        return result;
    }
}
