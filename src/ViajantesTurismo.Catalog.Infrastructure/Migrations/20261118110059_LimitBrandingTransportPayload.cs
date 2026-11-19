using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PandaBear.EntityFrameworkCore;

namespace ViajantesTurismo.Catalog.Infrastructure.Migrations;

/// <summary>Bounds the serialized payload of Branding updates routed to Admin.</summary>
[DbContext(typeof(CatalogDbContext))]
[Migration("20261118110059_LimitBrandingTransportPayload")]
public sealed class LimitBrandingTransportPayload : Migration
{
    private const string ConstraintName = "CK_transport_messages_branding_payload_size";
    private const string TransportTable = "transport_messages";

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The Catalog migration owns the transport table shared by the Admin and Catalog consumers.
        migrationBuilder.AddCheckConstraint(
            name: ConstraintName,
            table: TransportTable,
            schema: SharedKernelSchemas.Messaging,
            sql: """
                "ConsumerName" <> 'admin'
                OR "EventType" <> 'branding.settings.changed'
                OR octet_length("Payload") <= 65536
                """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: ConstraintName,
            table: TransportTable,
            schema: SharedKernelSchemas.Messaging);
    }
}
