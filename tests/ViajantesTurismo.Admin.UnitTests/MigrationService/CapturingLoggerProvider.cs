using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    public string? CategoryName { get; private set; }

    public ILogger CreateLogger(string categoryName)
    {
        CategoryName = categoryName;
        return NullLogger.Instance;
    }

    public void Dispose()
    {
    }
}
