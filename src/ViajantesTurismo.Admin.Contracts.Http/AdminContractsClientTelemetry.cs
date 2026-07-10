using System.Diagnostics;

namespace ViajantesTurismo.Admin.Contracts.Http;

internal static class AdminContractsClientTelemetry
{
    public const string Name = "ViajantesTurismo.Admin.Contracts.Clients";
    public const string CreateCustomerActivity = "customers.create";
    public const string CreateTourActivity = "tours.create";
    public const string CreateBookingActivity = "bookings.create";
    public const string RecordPaymentActivity = "bookings.payments.record";
    public const string ApiAreaTag = "viajantes.api_area";
    public const string OperationTag = "viajantes.operation";
    public const string StatusCodeTag = "http.response.status_code";
    public const string CommandOutcomeKindTag = "viajantes.admin_command.outcome";
    public const string AdminApiArea = "admin";

    public static ActivitySource ActivitySource { get; } = new(Name);
}
