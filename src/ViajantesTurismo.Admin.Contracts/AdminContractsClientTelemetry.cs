using System.Diagnostics;

namespace ViajantesTurismo.Admin.Contracts;

internal static class AdminContractsClientTelemetry
{
    public const string Name = "ViajantesTurismo.Admin.Contracts.Clients";
    public const string CreateCustomerActivity = "customers.create";
    public const string ApiAreaTag = "viajantes.api_area";
    public const string OperationTag = "viajantes.operation";
    public const string StatusCodeTag = "http.response.status_code";
    public const string OutcomeKindTag = "viajantes.customer_create.outcome";
    public const string AdminApiArea = "admin";

    public static ActivitySource ActivitySource { get; } = new(Name);
}
