using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Branding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "branding");

            migrationBuilder.CreateTable(
                name: "BrandingLogos",
                schema: "branding",
                columns: table => new
                {
                    Sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    StoredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandingLogos", x => x.Sha256);
                });

            migrationBuilder.CreateTable(
                name: "BrandingRecoveryAudits",
                schema: "branding",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OldEpoch = table.Column<Guid>(type: "uuid", nullable: false),
                    OldRevision = table.Column<long>(type: "bigint", nullable: false),
                    NewEpoch = table.Column<Guid>(type: "uuid", nullable: false),
                    NewRevision = table.Column<long>(type: "bigint", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IncidentReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Actor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandingRecoveryAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "branding",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnqueuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishAttempts = table.Column<int>(type: "integer", nullable: false),
                    LastPublishAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextPublishAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastPublishError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ClaimedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClaimedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EnvelopeSpec = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EnvelopeSpecVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EventId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Source = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventVersion = table.Column<int>(type: "integer", nullable: true),
                    Time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DataContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DataSchema = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Payload = table.Column<string>(type: "text", nullable: true),
                    PayloadEncoding = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExtensionAttributesJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BrandingSettings",
                schema: "branding",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PrimaryColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    AccentColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    BackgroundColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    TextColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    HeadingFontFamily = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    BodyFontFamily = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LogoUri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    LogoSha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    SourceEpoch = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceRevision = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandingSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrandingSettings_BrandingLogos_LogoSha256",
                        column: x => x.LogoSha256,
                        principalSchema: "branding",
                        principalTable: "BrandingLogos",
                        principalColumn: "Sha256",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BrandingSettingsRevisions",
                schema: "branding",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEpoch = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceRevision = table.Column<long>(type: "bigint", nullable: false),
                    BrandName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PrimaryColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    AccentColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    BackgroundColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    TextColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    HeadingFontFamily = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    BodyFontFamily = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LogoUri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    LogoSha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    CommittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandingSettingsRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrandingSettingsRevisions_BrandingLogos_LogoSha256",
                        column: x => x.LogoSha256,
                        principalSchema: "branding",
                        principalTable: "BrandingLogos",
                        principalColumn: "Sha256",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrandingSettings_LogoSha256",
                schema: "branding",
                table: "BrandingSettings",
                column: "LogoSha256");

            migrationBuilder.CreateIndex(
                name: "IX_BrandingSettingsRevisions_LogoSha256",
                schema: "branding",
                table: "BrandingSettingsRevisions",
                column: "LogoSha256");

            migrationBuilder.CreateIndex(
                name: "IX_BrandingSettingsRevisions_SourceEpoch_SourceRevision",
                schema: "branding",
                table: "BrandingSettingsRevisions",
                columns: new[] { "SourceEpoch", "SourceRevision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_EventId",
                schema: "branding",
                table: "outbox_messages",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_PublishedAt_EnqueuedAt",
                schema: "branding",
                table: "outbox_messages",
                columns: new[] { "PublishedAt", "EnqueuedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_PublishedAt_NextPublishAttemptAt_EnqueuedAt",
                schema: "branding",
                table: "outbox_messages",
                columns: new[] { "PublishedAt", "NextPublishAttemptAt", "EnqueuedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrandingRecoveryAudits",
                schema: "branding");

            migrationBuilder.DropTable(
                name: "BrandingSettings",
                schema: "branding");

            migrationBuilder.DropTable(
                name: "BrandingSettingsRevisions",
                schema: "branding");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "branding");

            migrationBuilder.DropTable(
                name: "BrandingLogos",
                schema: "branding");
        }
    }
}
