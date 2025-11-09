using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.ApiService;

internal static class ResultExtensions
{
    extension(Result result)
    {
        public ValidationProblem ToValidationProblem()
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

        public NotFound<ProblemDetails> ToNotFound()
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
    }

    extension<T>(Result<T> result)
    {
        public ValidationProblem ToValidationProblem()
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

        public NotFound<ProblemDetails> ToNotFound()
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
    }
}