namespace ViajantesTurismo.Management.Security;

/// <summary>
/// Defines storage names for Management security state.
/// </summary>
public static class ManagementSecurityDefaults
{
    /// <summary>
    /// Gets the PostgreSQL schema used for Management security state.
    /// </summary>
    public const string SchemaName = "security";

    /// <summary>
    /// Gets the PostgreSQL table used for protected Management session tickets.
    /// </summary>
    public const string TicketTableName = "management_cookie_tickets";
}
