using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

/// <summary>
/// Logger provider that remembers the last requested category name and discards log output.
/// </summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    /// <summary>
    /// Gets the category name from the last <see cref="CreateLogger"/> call, or <see langword="null"/> if none.
    /// </summary>
    public string? CategoryName
    {
        get; private set;
    }

    /// <summary>
    /// Records <paramref name="categoryName"/> and returns a no-op logger.
    /// </summary>
    public ILogger CreateLogger(string categoryName)
    {
        CategoryName = categoryName;
        return NullLogger.Instance;
    }

    /// <summary>
    /// Releases no resources.
    /// </summary>
    public void Dispose()
    {
    }
}
