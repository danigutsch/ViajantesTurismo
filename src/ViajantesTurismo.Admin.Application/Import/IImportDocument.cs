namespace ViajantesTurismo.Admin.Application.Import;

/// <summary>
/// Represents a tabular document that can be imported into the system.
/// </summary>
/// <remarks>
/// Implementations expose the parsed header row via <see cref="Headers"/> and the data rows via
/// <see cref="Rows"/>. Each row is expected to be aligned with the header list by index.
/// <para>
/// This abstraction is format-agnostic and can be implemented by CSV, Excel, or other tabular sources.
/// The concrete parser should handle format-specific validation (for example, required-header checks).
/// </para>
/// </remarks>
public interface IImportDocument
{
    /// <summary>
    /// Gets the header columns from the imported document.
    /// </summary>
    IReadOnlyList<string> Headers { get; }

    /// <summary>
    /// Gets the data rows from the imported document.
    /// </summary>
    IReadOnlyList<IImportRow> Rows { get; }
}
