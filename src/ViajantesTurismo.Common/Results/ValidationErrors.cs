namespace ViajantesTurismo.Common.Results;

/// <summary>
/// Collects and combines multiple validation errors into a single result.
/// </summary>
public sealed class ValidationErrors
{
    private const string MultipleValidationErrorsDetailMessage = "Multiple validation errors occurred.";

    private readonly List<Result> _errors = [];

    /// <summary>
    /// Gets a value indicating whether any validation errors have been collected.
    /// </summary>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>
    /// Returns the combined validation error result (non-generic).
    /// </summary>
    /// <returns>A combined error result with all validation errors.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no errors have been collected.</exception>
    public Result ToResult()
    {
        if (_errors.Count == 0)
        {
            throw new InvalidOperationException("Cannot create result from empty error collection. Check HasErrors before calling ToResult.");
        }

        if (_errors.Count == 1)
        {
            return _errors[0];
        }

        var mergedErrors = MergeValidationErrors();
        return Result.Invalid(MultipleValidationErrorsDetailMessage, mergedErrors);
    }

    /// <summary>
    /// Returns the combined validation error result as Result&lt;T&gt;.
    /// </summary>
    /// <typeparam name="T">The type of value being validated.</typeparam>
    /// <returns>A combined error result with all validation errors.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no errors have been collected.</exception>
    public Result<T> ToResult<T>() where T : notnull
    {
        if (_errors.Count == 0)
        {
            throw new InvalidOperationException("Cannot create result from empty error collection. Check HasErrors before calling ToResult.");
        }

        if (_errors.Count == 1)
        {
            var singleError = _errors[0];
            return Result<T>.Invalid(
                singleError.ErrorDetails!.Detail,
                singleError.ErrorDetails.ValidationErrors!);
        }

        var mergedErrors = MergeValidationErrors();
        return Result<T>.Invalid(MultipleValidationErrorsDetailMessage, mergedErrors);
    }

    private Dictionary<string, string[]> MergeValidationErrors()
    {
        var mergedValidationErrors = new Dictionary<string, List<string>>();

        foreach (var error in _errors)
        {
            var validationErrors = error.ErrorDetails?.ValidationErrors;
            if (validationErrors is null)
            {
                continue;
            }

            foreach (var (field, messages) in validationErrors)
            {
                if (!mergedValidationErrors.TryGetValue(field, out var existingMessages))
                {
                    existingMessages = [];
                    mergedValidationErrors[field] = existingMessages;
                }

                existingMessages.AddRange(messages);
            }
        }

        return mergedValidationErrors.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray()
        );
    }

    /// <summary>
    /// Adds a validation error to the collection.
    /// </summary>
    /// <param name="error">The validation error result to add.</param>
    /// <exception cref="InvalidOperationException">Thrown if the result status is not Invalid.</exception>
    public void Add(Result error)
    {
        if (error.Status != ResultStatus.Invalid)
        {
            throw new InvalidOperationException("Only validation errors can be added.");
        }

        _errors.Add(error);
    }

    /// <summary>
    /// Adds a validation error to the collection from a Result&lt;T&gt;.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="error">The validation error result to add.</param>
    /// <exception cref="InvalidOperationException">Thrown if the result status is not Invalid.</exception>
    public void Add<T>(Result<T> error) where T : notnull
    {
        if (error.Status != ResultStatus.Invalid)
        {
            throw new InvalidOperationException("Only validation errors can be added.");
        }

        _errors.Add(error.ToResult());
    }
}
