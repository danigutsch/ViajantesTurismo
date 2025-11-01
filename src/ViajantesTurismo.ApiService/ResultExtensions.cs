using Microsoft.AspNetCore.Http.HttpResults;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.ApiService;

internal static class ResultExtensions
{
    public static ValidationProblem ToValidationProblem(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot convert a successful result to a ValidationProblem.");
        }

        if (result.Status != ResultStatus.Invalid)
        {
            throw new InvalidOperationException("Only results with status 'Invalid' can be converted to a ValidationProblem.");
        }

        if (result.ErrorDetails is null)
        {
            throw new InvalidOperationException("Error details are required to convert to a ValidationProblem.");
        }

        if (result.ErrorDetails.ValidationErrors is null)
        {
            throw new InvalidOperationException("Validation errors are required to convert to a ValidationProblem.");
        }

        return TypedResults.ValidationProblem(result.ErrorDetails.ValidationErrors, result.ErrorDetails.Detail);
    }

    public static ValidationProblem ToValidationProblem<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot convert a successful result to a ValidationProblem.");
        }

        if (result.Status != ResultStatus.Invalid)
        {
            throw new InvalidOperationException("Only results with status 'Invalid' can be converted to a ValidationProblem.");
        }

        if (result.ErrorDetails is null)
        {
            throw new InvalidOperationException("Error details are required to convert to a ValidationProblem.");
        }

        if (result.ErrorDetails.ValidationErrors is null)
        {
            throw new InvalidOperationException("Validation errors are required to convert to a ValidationProblem.");
        }

        return TypedResults.ValidationProblem(result.ErrorDetails.ValidationErrors, result.ErrorDetails.Detail);
    }
}
