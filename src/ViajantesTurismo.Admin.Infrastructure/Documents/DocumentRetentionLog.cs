using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Log messages shared by the document retention workers; counts only, no identifiers.</summary>
internal static partial class DocumentRetentionLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Retention run {Service} skipped {HeldCount} expired candidates restrained by a disposition hold.")]
    public static partial void SkippedHeld(ILogger logger, string service, int heldCount);
}
