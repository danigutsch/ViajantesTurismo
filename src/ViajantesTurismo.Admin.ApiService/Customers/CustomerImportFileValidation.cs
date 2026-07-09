using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.ApiService.Customers;

internal static class CustomerImportFileValidation
{
    public const long MaxFileBytes = 1_048_576;

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
        return IsAllowedContentType(file.ContentType)
            && Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAllowedContentType(string contentType)
    {
        return ContractConstants.CustomerImportAllowedContentTypes.Any(allowedContentType =>
            allowedContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase));
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
