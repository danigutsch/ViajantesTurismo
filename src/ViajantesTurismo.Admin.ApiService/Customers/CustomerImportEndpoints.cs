using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SharedKernel.MalwareScanning;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.Admin.ApiService.Customers;

/// <summary>
/// Defines endpoints for bulk customer import operations.
/// </summary>
internal static class CustomerImportEndpoints
{
    private const string InvalidScannerResultMessage = "Customer import file did not pass malware scanning.";
    private const string ScannerUnavailableMessage = "Customer import scanner is unavailable.";

    /// <summary>
    /// Maps all customer import endpoints to the application.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    public static WebApplication MapCustomerImportEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var importGroup = app.MapCustomerImportsGroup()
            .RequireRateLimiting(AdminSecurityBaseline.ImportRateLimitPolicy)
            .RequireAuthorization(AdminAuthorization.CustomerImport);

        importGroup.MapPost("/", ImportCustomers)
            .WithName("ImportCustomers")
            .WithDescription("Imports customers from a CSV file.")
            .WithSummary("Imports customers from a CSV file.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithMetadata(new RequestSizeLimitAttribute(ContractConstants.CustomerImportMaxRequestBytes))
            .DisableAntiforgery();

        importGroup.MapPost("/commit", CommitImportWithResolutions)
            .WithName("CommitImportWithResolutions")
            .WithDescription("Commits customer import applying conflict resolutions.")
            .WithSummary("Commits customer import applying conflict resolutions.")
            .Accepts<CommitCustomerImportFormDto>("multipart/form-data")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithMetadata(new RequestSizeLimitAttribute(ContractConstants.CustomerImportMaxRequestBytes))
            .DisableAntiforgery();

        return app;
    }

    private static async Task<Results<Ok<ImportResultDto>, ProblemHttpResult>> ImportCustomers(
        [Required]
        IFormFile? file,
        [FromServices] IMalwareScanner scanner,
        [FromServices] CustomerImportWorkflowService workflow,
        CancellationToken ct)
    {
        if (!TryValidateImportFile(file, out var problem))
        {
            return TypedResults.Problem(
                detail: problem.Detail,
                title: problem.Title,
                statusCode: problem.Status);
        }

        var csvResult = await ReadCsv(file, scanner, ct);
        if (csvResult.IsFailure)
        {
            return CreateScanProblem(csvResult);
        }

        var result = await workflow.Import(csvResult.Value, ct);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<ImportResultDto>, ProblemHttpResult>> CommitImportWithResolutions(
        [AsParameters] CommitCustomerImportFormDto form,
        [FromServices] IMalwareScanner scanner,
        [FromServices] CustomerImportWorkflowService workflow,
        CancellationToken ct)
    {
        if (!TryValidateImportFile(form.File, out var problem))
        {
            return TypedResults.Problem(
                detail: problem.Detail,
                title: problem.Title,
                statusCode: problem.Status);
        }

        var csvResult = await ReadCsv(form.File, scanner, ct);
        if (csvResult.IsFailure)
        {
            return CreateScanProblem(csvResult);
        }

        var parsedConflictResolutions = ConflictResolutionSerialization.Parse(form.ConflictResolutions);
        var result = await workflow.Commit(
            csvResult.Value,
            parsedConflictResolutions,
            ct);

        return TypedResults.Ok(result);
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Untrusted uploads must fail closed when a scanner implementation cannot make a decision.")]
    internal static async Task<Result<string>> ReadCsv(IFormFile file, IMalwareScanner scanner, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(scanner);

        using var content = new MemoryStream();
        await using var source = file.OpenReadStream();
        if (!await CopyToMemory(source, content, ContractConstants.CustomerImportMaxFileBytes, ct).ConfigureAwait(false)
            || content.Length != file.Length)
        {
            return Result.Invalid<string>(
                "Invalid customer import file.",
                nameof(file),
                "Upload a CSV file that meets the documented import requirements.");
        }

        content.Position = 0;
        MalwareScanResult scanResult;
        try
        {
            scanResult = await scanner.Scan(content, content.Length, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result.Unavailable<string>(ScannerUnavailableMessage);
        }

        if (scanResult.Status is MalwareScanStatus.Rejected)
        {
            return Result.Invalid<string>(InvalidScannerResultMessage, nameof(file), InvalidScannerResultMessage);
        }

        if (scanResult.Status is not (MalwareScanStatus.Passed or MalwareScanStatus.Disabled))
        {
            return Result.Unavailable<string>(ScannerUnavailableMessage);
        }

        content.Position = 0;
        using var reader = new StreamReader(content, leaveOpen: true);
        return Result.Ok(await reader.ReadToEndAsync(ct).ConfigureAwait(false));
    }

    private static ProblemHttpResult CreateScanProblem(Result<string> result)
    {

        var isInvalid = result.Status is ResultStatus.Invalid;
        return TypedResults.Problem(
            detail: result.ErrorDetails?.Detail ?? ScannerUnavailableMessage,
            title: isInvalid ? "Invalid customer import file." : "Customer import scanner unavailable.",
            statusCode: isInvalid ? StatusCodes.Status400BadRequest : StatusCodes.Status503ServiceUnavailable);
    }

    private static async ValueTask<bool> CopyToMemory(Stream source, MemoryStream destination, long maximumLength, CancellationToken ct)
    {
        var buffer = new byte[81920];
        while (true)
        {
            var bytesRead = await source.ReadAsync(buffer, ct).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                destination.Position = 0;
                return true;
            }

            if (destination.Length + bytesRead > maximumLength)
            {
                destination.Position = 0;
                return false;
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
        }
    }

    internal static bool TryValidateImportFile([NotNullWhen(true)] IFormFile? file, [NotNullWhen(false)] out ProblemDetails? problem)
    {
        if (file is null || file.Length <= 0 || file.Length > ContractConstants.CustomerImportMaxFileBytes || !IsAllowedCsv(file))
        {
            problem = CreateImportFileProblem();
            return false;
        }

        problem = null;
        return true;
    }

    private static bool IsAllowedCsv(IFormFile file)
    {
        var contentType = MediaTypeHeaderValue.TryParse(file.ContentType, out var mediaType)
            ? mediaType.MediaType.Value
            : file.ContentType;

        return ContractConstants.CustomerImportAllowedContentTypes.Any(allowedContentType =>
                allowedContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
            && Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase);
    }

    private static ProblemDetails CreateImportFileProblem()
    {
        return new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid customer import file.",
            Detail = "Upload a CSV file that meets the documented import requirements."
        };
    }
}
