using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "messaging");

            migrationBuilder.CreateTable(
                name: "CatalogTourReadModels",
                columns: table => new
                {
                    CatalogTourId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminTourId = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    Position = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogTourReadModels", x => x.CatalogTourId);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                schema: "messaging",
                columns: table => new
                {
                    Scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResultFingerprint = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_keys", x => new { x.Scope, x.Key });
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "messaging",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnqueuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishAttempts = table.Column<int>(type: "integer", nullable: false),
                    LastPublishAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextPublishAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastPublishError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                name: "PublicContent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceLanguage = table.Column<string>(type: "text", nullable: false),
                    PublicationState = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicContent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicMediaImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceObjectKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    ProcessingStatus = table.Column<string>(type: "text", nullable: false),
                    AltText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Caption = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Attribution = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Copyright = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Tags = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicMediaImages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicThemeSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrimaryColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    AccentColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    BackgroundColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    TextColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    HeadingFontFamily = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BodyFontFamily = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicThemeSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicContentVariants",
                columns: table => new
                {
                    Language = table.Column<string>(type: "text", nullable: false),
                    PublicContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SeoTitle = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    MetaDescription = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ShareSummary = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RequiresHumanReview = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicContentVariants", x => new { x.PublicContentId, x.Language });
                    table.ForeignKey(
                        name: "FK_PublicContentVariants_PublicContent_PublicContentId",
                        column: x => x.PublicContentId,
                        principalTable: "PublicContent",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PublicMediaImageResponsiveVariants",
                columns: table => new
                {
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    PublicMediaImageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicMediaImageResponsiveVariants", x => new { x.PublicMediaImageId, x.SortOrder });
                    table.ForeignKey(
                        name: "FK_PublicMediaImageResponsiveVariants_PublicMediaImages_Public~",
                        column: x => x.PublicMediaImageId,
                        principalTable: "PublicMediaImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PublicMediaImageTourLinks",
                columns: table => new
                {
                    CatalogTourId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicMediaImageId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsCover = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicMediaImageTourLinks", x => new { x.PublicMediaImageId, x.CatalogTourId });
                    table.ForeignKey(
                        name: "FK_PublicMediaImageTourLinks_PublicMediaImages_PublicMediaImag~",
                        column: x => x.PublicMediaImageId,
                        principalTable: "PublicMediaImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogTourReadModels_AdminTourId",
                table: "CatalogTourReadModels",
                column: "AdminTourId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogTourReadModels_Slug",
                table: "CatalogTourReadModels",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_EventId",
                schema: "messaging",
                table: "outbox_messages",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_PublishedAt_EnqueuedAt",
                schema: "messaging",
                table: "outbox_messages",
                columns: new[] { "PublishedAt", "EnqueuedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_PublishedAt_NextPublishAttemptAt_EnqueuedAt",
                schema: "messaging",
                table: "outbox_messages",
                columns: new[] { "PublishedAt", "NextPublishAttemptAt", "EnqueuedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicContent_Key",
                table: "PublicContent",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicMediaImageTourLinks_CatalogTourId_DisplayOrder",
                table: "PublicMediaImageTourLinks",
                columns: new[] { "CatalogTourId", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogTourReadModels");

            migrationBuilder.DropTable(
                name: "idempotency_keys",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "PublicContentVariants");

            migrationBuilder.DropTable(
                name: "PublicMediaImageResponsiveVariants");

            migrationBuilder.DropTable(
                name: "PublicMediaImageTourLinks");

            migrationBuilder.DropTable(
                name: "PublicThemeSettings");

            migrationBuilder.DropTable(
                name: "PublicContent");

            migrationBuilder.DropTable(
                name: "PublicMediaImages");
        }
    }
}
