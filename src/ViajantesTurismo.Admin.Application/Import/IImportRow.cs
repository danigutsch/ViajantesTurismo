namespace ViajantesTurismo.Admin.Application.Import;

/// <summary>
/// Represents a single row of data to be imported, providing access by column index and header name.
/// Implementations may vary based on the source of the data (e.g., CSV, Excel, etc.).
/// </summary>
public interface IImportRow
{
    /// <summary>
    /// Gets the number of columns in the row.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the value at the specified column index.
    /// </summary>
    /// <param name="index">The zero-based column index.</param>
    /// <returns>The value at the index.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is out of range.</exception>
    string this[int index] { get; }

    /// <summary>
    /// Gets a row value by header name using the provided headers.
    /// </summary>
    /// <param name="headers">The header columns aligned with this row.</param>
    /// <param name="headerName">The header name to resolve.</param>
    /// <returns>The row value under the given header.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="headers"/> or <paramref name="headerName"/> is null.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the specified header does not exist.</exception>
    string this[IReadOnlyList<string> headers, string headerName] { get; }

    /// <summary>
    /// Tries to get a row value by the header name.
    /// </summary>
    /// <param name="headers">The header columns aligned with this row.</param>
    /// <param name="headerName">The header name to resolve.</param>
    /// <param name="value">The resolved value when found; otherwise null.</param>
    /// <returns>True when the header exists; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="headers"/> or <paramref name="headerName"/> is null.</exception>
    bool TryGetByHeader(IReadOnlyList<string> headers, string headerName, out string? value);
}
