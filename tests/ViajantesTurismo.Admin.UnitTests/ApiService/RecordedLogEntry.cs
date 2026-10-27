using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

/// <summary>One entry captured by <see cref="RecordingLogger"/>.</summary>
internal sealed record RecordedLogEntry
{
    /// <summary>Gets the severity.</summary>
    public required LogLevel Level
    {
        get; init;
    }

    /// <summary>Gets the event identifier.</summary>
    public required EventId EventId
    {
        get; init;
    }

    /// <summary>Gets the formatted message.</summary>
    public required string Message
    {
        get; init;
    }

    /// <summary>Gets the structured key-value pairs of the log state.</summary>
    public required KeyValuePair<string, object?>[] Values
    {
        get; init;
    }

    /// <summary>Gets the exception passed to the logger, if any.</summary>
    public Exception? Exception
    {
        get; init;
    }
}
