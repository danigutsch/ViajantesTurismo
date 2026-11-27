using Microsoft.Extensions.Logging;
using ViajantesTurismo.Admin.Application.Payments;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

/// <summary>Models explicit test-only log inputs and the existing metadata-only policy.</summary>
internal static partial class PaymentCommandDiagnosticLogProbe
{
    private static readonly Action<ILogger, IPaymentCommand, Exception?> WholeObject =
        LoggerMessage.Define<IPaymentCommand>(LogLevel.Information, new EventId(1), "Command {Command}");

    private static readonly Action<ILogger, IPaymentCommand, Exception?> DestructuringPlaceholder =
        LoggerMessage.Define<IPaymentCommand>(LogLevel.Information, new EventId(2), "Command {@Command}");

    /// <summary>Passes a synthetic payment command through the whole-object logging control.</summary>
    internal static void CaptureWholeObject(ILogger logger, IPaymentCommand value, bool destructuring)
    {
        var write = destructuring ? DestructuringPlaceholder : WholeObject;
        write(logger, value, null);
    }

    /// <summary>Records only technical scalar information about a failure.</summary>
    [LoggerMessage(3, LogLevel.Error, "Command {RequestName} finished as {Outcome}. Error type: {ErrorType}.")]
    internal static partial void ReportFailure(ILogger logger, string requestName, string outcome, string errorType);
}
