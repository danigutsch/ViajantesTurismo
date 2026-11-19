using Microsoft.AspNetCore.Http.HttpResults;
using PandaBear.Results;

namespace ViajantesTurismo.Catalog.Api;

/// <summary>Maps Catalog validation failures to the API error contract.</summary>
internal static class CatalogValidationProblems
{
    /// <summary>Maps validation errors from a Catalog result to an HTTP validation problem.</summary>
    internal static ValidationProblem ToValidationProblem(ResultError error)
    {
        return TypedResults.ValidationProblem(ToValidationProblemDictionary(error.ValidationErrors), detail: error.Detail);
    }

    /// <summary>Copies validation fields and messages into the shape used by HTTP results.</summary>
    internal static Dictionary<string, string[]> ToValidationProblemDictionary(IReadOnlyDictionary<string, IReadOnlyList<string>>? validationErrors)
    {
        if (validationErrors is null)
        {
            return [];
        }

        var result = new Dictionary<string, string[]>(validationErrors.Count, StringComparer.Ordinal);
        foreach (var (field, messages) in validationErrors)
        {
            result[field] = [.. messages];
        }

        return result;
    }
}
