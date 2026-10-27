using System.Diagnostics;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Telemetry names and tags for the Admin HTTP contract clients.</summary>
internal static class AdminContractsClientTelemetry
{
    /// <summary>Name of the activity source used by the Admin contract clients.</summary>
    public const string Name = "ViajantesTurismo.Admin.Contracts.Clients";
    /// <summary>Activity name for creating a customer.</summary>
    public const string CreateCustomerActivity = "customers.create";
    /// <summary>Activity name for creating a tour.</summary>
    public const string CreateTourActivity = "tours.create";
    /// <summary>Activity name for creating a booking.</summary>
    public const string CreateBookingActivity = "bookings.create";
    /// <summary>Tag key identifying the API area of a client call.</summary>
    public const string ApiAreaTag = "viajantes.api_area";
    /// <summary>Tag key identifying the client operation.</summary>
    public const string OperationTag = "viajantes.operation";
    /// <summary>Tag key carrying the HTTP response status code.</summary>
    public const string StatusCodeTag = "http.response.status_code";
    /// <summary>Tag key carrying the Admin command outcome kind.</summary>
    public const string CommandOutcomeKindTag = "viajantes.admin_command.outcome";
    /// <summary>Tag value identifying the Admin API area.</summary>
    public const string AdminApiArea = "admin";

    /// <summary>Activity source for Admin contract client calls.</summary>
    public static ActivitySource ActivitySource { get; } = new(Name);
}
