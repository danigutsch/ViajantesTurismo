namespace ViajantesTurismo.Common.Results;

/// <summary>
/// Collects and combines multiple validation errors into a single result.
/// </summary>
/// <typeparam name="T">The type of value being validated.</typeparam>
public sealed class ValidationErrors<T>
{
    private const string MultipleValidationErrorsDetailMessage = "Multiple validation errors occurred.";

    private readonly List<Result<T>> _errors = [];

    /// <summary>
    /// Gets a value indicating whether any validation errors have been collected.
    /// </summary>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>
    /// Returns the combined validation error result. If multiple errors exist, they are merged into a single result. Should only be called if <see cref="HasErrors"/> is true.
    /// </summary>
    /// <returns>A combined error result with all validation errors.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no errors have been collected.</exception>
    public Result<T> ToResult()
    {
        if (_errors.Count == 0)
        {
            throw new InvalidOperationException("Cannot create result from empty error collection. Check HasErrors before calling ToResult.");
        }

        if (_errors.Count == 1)
        {
            return _errors[0];
        }

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

        var finalValidationErrors = mergedValidationErrors.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray()
        );

        return Result<T>.Invalid(MultipleValidationErrorsDetailMessage, finalValidationErrors);
    }

    /// <summary>
    /// Adds a validation error to the collection.
    /// </summary>
    /// <param name="error">The validation error result to add.</param>
    /// <exception cref="InvalidOperationException">Thrown if the result status is not Invalid.</exception>
    public void Add(Result<T> error)
    {
        if (error.Status != ResultStatus.Invalid)
        {
            throw new InvalidOperationException("Only validation errors can be added.");
        }

        _errors.Add(error);
    }
}
