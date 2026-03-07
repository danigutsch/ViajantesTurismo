namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Represents the aggregated outcome of a customer import operation.
/// </summary>
public readonly record struct ImportResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImportResult"/> struct.
    /// </summary>
    /// <param name="successCount">Number of successfully imported rows.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="successCount"/> is negative.</exception>
    public ImportResult(int successCount)
        : this(successCount, 0)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportResult"/> struct.
    /// </summary>
    /// <param name="successCount">Number of successfully imported rows.</param>
    /// <param name="errorCount">Number of rows that failed during import.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="successCount"/> or <paramref name="errorCount"/> is negative.
    /// </exception>
    public ImportResult(int successCount, int errorCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(successCount);
        ArgumentOutOfRangeException.ThrowIfNegative(errorCount);
        SuccessCount = successCount;
        ErrorCount = errorCount;
    }

    /// <summary>
    /// Number of successfully imported rows.
    /// </summary>
    public int SuccessCount { get; }

    /// <summary>
    /// Number of rows that failed during import.
    /// </summary>
    public int ErrorCount { get; }
}
