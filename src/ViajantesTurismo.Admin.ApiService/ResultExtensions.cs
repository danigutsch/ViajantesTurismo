using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.ApiService;

/// <summary>
/// Extension methods for converting <see cref="Result"/> and <see cref="Result{T}"/> to typed HTTP results.
/// </summary>
internal static class ResultExtensions
{
    /// <summary>
    /// Converts a failed <see cref="Result"/> with status <see cref="ResultStatus.Invalid"/> to a <see cref="ValidationProblem"/>.
    /// </summary>
    /// <param name="result">The failed result containing validation errors.</param>
    /// <returns>A <see cref="ValidationProblem"/> with the validation error details.</returns>
    /// <exception cref="InvalidOperationException">The result is successful or does not have status <see cref="ResultStatus.Invalid"/>.</exception>
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

    /// <summary>
    /// Converts a failed <see cref="Result{T}"/> with status <see cref="ResultStatus.Invalid"/> to a <see cref="ValidationProblem"/>.
    /// </summary>
    /// <typeparam name="T">The value type of the result.</typeparam>
    /// <param name="result">The failed result containing validation errors.</param>
    /// <returns>A <see cref="ValidationProblem"/> with the validation error details.</returns>
    /// <exception cref="InvalidOperationException">The result is successful or does not have status <see cref="ResultStatus.Invalid"/>.</exception>
    public static ValidationProblem ToValidationProblem<T>(this Result<T> result) where T : notnull
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

    /// <summary>
    /// Converts a failed <see cref="Result"/> with status <see cref="ResultStatus.NotFound"/> to a <see cref="NotFound{ProblemDetails}"/>.
    /// </summary>
    /// <param name="result">The failed result.</param>
    /// <returns>A 404 response with problem details.</returns>
    /// <exception cref="InvalidOperationException">The result is successful or does not have status <see cref="ResultStatus.NotFound"/>.</exception>
    public static NotFound<ProblemDetails> ToNotFound(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot convert a successful result to NotFound.");
        }

        if (result.Status != ResultStatus.NotFound)
        {
            throw new InvalidOperationException("Only results with status 'NotFound' can be converted to NotFound.");
        }

        var problemDetails = new ProblemDetails
        {
            Title = "Resource Not Found",
            Detail = result.ErrorDetails?.Detail,
            Status = StatusCodes.Status404NotFound
        };

        return TypedResults.NotFound(problemDetails);
    }

    /// <summary>
    /// Converts a failed <see cref="Result{T}"/> with status <see cref="ResultStatus.NotFound"/> to a <see cref="NotFound{ProblemDetails}"/>.
    /// </summary>
    /// <typeparam name="T">The value type of the result.</typeparam>
    /// <param name="result">The failed result.</param>
    /// <returns>A 404 response with problem details.</returns>
    /// <exception cref="InvalidOperationException">The result is successful or does not have status <see cref="ResultStatus.NotFound"/>.</exception>
    public static NotFound<ProblemDetails> ToNotFound<T>(this Result<T> result) where T : notnull
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot convert a successful result to NotFound.");
        }

        if (result.Status != ResultStatus.NotFound)
        {
            throw new InvalidOperationException("Only results with status 'NotFound' can be converted to NotFound.");
        }

        var problemDetails = new ProblemDetails
        {
            Title = "Resource Not Found",
            Detail = result.ErrorDetails?.Detail,
            Status = StatusCodes.Status404NotFound
        };

        return TypedResults.NotFound(problemDetails);
    }

    /// <summary>
    /// Converts a failed <see cref="Result"/> with status <see cref="ResultStatus.Conflict"/> to a <see cref="Conflict{ProblemDetails}"/>.
    /// </summary>
    /// <param name="result">The failed result.</param>
    /// <returns>A 409 response with problem details.</returns>
    /// <exception cref="InvalidOperationException">The result is successful or does not have status <see cref="ResultStatus.Conflict"/>.</exception>
    public static Conflict<ProblemDetails> ToConflict(this Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot convert a successful result to Conflict.");
        }

        if (result.Status != ResultStatus.Conflict)
        {
            throw new InvalidOperationException("Only results with status 'Conflict' can be converted to Conflict.");
        }

        var problemDetails = new ProblemDetails
        {
            Title = "Conflict",
            Detail = result.ErrorDetails?.Detail,
            Status = StatusCodes.Status409Conflict
        };

        return TypedResults.Conflict(problemDetails);
    }

    /// <summary>
    /// Converts a failed <see cref="Result{T}"/> with status <see cref="ResultStatus.Conflict"/> to a <see cref="Conflict{ProblemDetails}"/>.
    /// </summary>
    /// <typeparam name="T">The value type of the result.</typeparam>
    /// <param name="result">The failed result.</param>
    /// <returns>A 409 response with problem details.</returns>
    /// <exception cref="InvalidOperationException">The result is successful or does not have status <see cref="ResultStatus.Conflict"/>.</exception>
    public static Conflict<ProblemDetails> ToConflict<T>(this Result<T> result) where T : notnull
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot convert a successful result to Conflict.");
        }

        if (result.Status != ResultStatus.Conflict)
        {
            throw new InvalidOperationException("Only results with status 'Conflict' can be converted to Conflict.");
        }

        var problemDetails = new ProblemDetails
        {
            Title = "Conflict",
            Detail = result.ErrorDetails?.Detail,
            Status = StatusCodes.Status409Conflict
        };

        return TypedResults.Conflict(problemDetails);
    }
}
