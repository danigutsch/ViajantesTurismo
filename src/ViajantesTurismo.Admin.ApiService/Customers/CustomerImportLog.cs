namespace ViajantesTurismo.Admin.ApiService.Customers;

/// <summary>Logs customer import outcomes that are handled without reaching the caller.</summary>
internal static partial class CustomerImportLog
{
    [LoggerMessage(
        EventId = 1,
        EventName = nameof(CustomerImportScannerFailed),
        Level = LogLevel.Warning,
        Message = "Customer import scanner failed; the import was refused. Failure type: {FailureType}.")]
    public static partial void CustomerImportScannerFailed(this ILogger logger, string failureType);
}
