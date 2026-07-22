using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.ServiceDefaults.Tests;

internal static partial class PrivacyTestLogger
{
    private static readonly Action<ILogger, Exception, string, Exception?> LogStructuredFailureCore =
        LoggerMessage.Define<Exception, string>(
            LogLevel.Error,
            new EventId(1, nameof(LogStructuredFailure)),
            "Operation failed with {DiagnosticFailure} and {Outcome}");

    private static readonly Action<ILogger, string, string, string, Exception?> LogEntityFrameworkCommandCore =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(20101, "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted"),
            "Executed DbCommand {CommandText} with {Parameters}; {Outcome}");

    private static readonly Action<ILogger, string, Exception?> LogMixedCaseExceptionTypeCore =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2, nameof(LogMixedCaseExceptionType)),
            "Operation failed with {Exception.Type}");

    private static readonly Action<ILogger, string, Exception?> LogMixedCaseErrorTypeCore =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(3, nameof(LogMixedCaseErrorType)),
            "Operation failed with {Error.Type}");

    [LoggerMessage(LogLevel.Error, "Customer {CustomerId} booking {BookingId} email {CustomerEmail} object {ObjectKey} failed with {Outcome}")]
    public static partial void LogFailure(
        ILogger logger,
        Exception exception,
        Guid customerId,
        Guid bookingId,
        string customerEmail,
        string objectKey,
        string outcome);

    public static void LogStructuredFailure(
        ILogger logger,
        Exception diagnosticFailure,
        string outcome)
    {
        LogStructuredFailureCore(logger, diagnosticFailure, outcome, null);
    }

    public static void LogEntityFrameworkCommand(
        ILogger logger,
        string commandText,
        string parameters,
        string outcome)
    {
        LogEntityFrameworkCommandCore(logger, commandText, parameters, outcome, null);
    }

    public static void LogMixedCaseExceptionType(ILogger logger, string exceptionType, Exception? exception)
    {
        LogMixedCaseExceptionTypeCore(logger, exceptionType, exception);
    }

    public static void LogMixedCaseErrorType(ILogger logger, string errorType)
    {
        LogMixedCaseErrorTypeCore(logger, errorType, null);
    }
}
