using Microsoft.AspNetCore.Mvc;

namespace ViajantesTurismo.Admin.ApiService.Customers;

internal static class CustomerImportFileValidation
{
    public const long MaxFileBytes = 1_048_576;

    private static readonly string[] AllowedContentTypes =
    [
        "text/csv",
        "application/csv",
        "application/vnd.ms-excel"
    ];

    public static bool TryValidate(IFormFile file, out ProblemDetails problem)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length <= 0 || file.Length > MaxFileBytes || !IsAllowedCsv(file))
        {
            problem = CreateProblem();
            return false;
        }

        problem = null!;
        return true;
    }

    private static bool IsAllowedCsv(IFormFile file)
    {
        return AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase)
            && Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase);
    }

    private static ProblemDetails CreateProblem()
    {
        return new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid customer import file.",
            Detail = "Upload a CSV file that meets the documented import requirements."
        };
    }
}
