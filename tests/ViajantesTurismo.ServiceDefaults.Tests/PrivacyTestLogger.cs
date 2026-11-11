using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.ServiceDefaults.Tests;

/// <summary>Log messages used to verify telemetry privacy redaction.</summary>
internal static partial class PrivacyTestLogger
{
    private static readonly Action<ILogger, Exception, string, Exception?> LogStructuredFailureCore =
        LoggerMessage.Define<Exception, string>(
            LogLevel.Error,
            new EventId(1, nameof(LogStructuredFailure)),
            "Operation failed with {DiagnosticFailure} and {Outcome}");

    [LoggerMessage(LogLevel.Warning, "No XML encryptor configured. Key {KeyId:B} may be persisted to storage in unencrypted form.")]
    public static partial void LogDataProtectionKey(ILogger logger, Guid keyId);

    [LoggerMessage(LogLevel.Error, "Customer {CustomerId} booking {BookingId} email {CustomerEmail} object {ObjectKey} failed with {Outcome}")]
    public static partial void LogFailure(
        ILogger logger,
        Exception exception,
        Guid customerId,
        Guid bookingId,
        string customerEmail,
        string objectKey,
        string outcome);

    /// <summary>Logs an error with an exception and outcome as structured values.</summary>
    public static void LogStructuredFailure(ILogger logger, Exception diagnosticFailure, string outcome)
    {
        LogStructuredFailureCore(logger, diagnosticFailure, outcome, null);
    }
}
