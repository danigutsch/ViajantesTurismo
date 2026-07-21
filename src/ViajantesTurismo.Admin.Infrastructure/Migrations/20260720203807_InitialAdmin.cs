using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "messaging");

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentAuditRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentRevision = table.Column<int>(type: "integer", nullable: true),
                    Operation = table.Column<string>(type: "text", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    ReasonCode = table.Column<string>(type: "text", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetentionExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAuditRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentLineages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Audience = table.Column<string>(type: "text", nullable: false),
                    HighestFinalizedRevision = table.Column<int>(type: "integer", nullable: false),
                    HighestRevision = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentLineages", x => x.Id);
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
                name: "Tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    DoubleRoomSupplementPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    RegularBikePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    EBikePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    MinCustomers = table.Column<int>(type: "integer", nullable: false),
                    MaxCustomers = table.Column<int>(type: "integer", nullable: false),
                    IncludedServices = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tours", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "transport_messages",
                schema: "messaging",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConsumeAttempts = table.Column<int>(type: "integer", nullable: false),
                    LastConsumeAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextConsumeAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastConsumeError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_transport_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAccommodationPreferences",
                columns: table => new
                {
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomType = table.Column<int>(type: "integer", nullable: false),
                    BedType = table.Column<int>(type: "integer", nullable: false),
                    CompanionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAccommodationPreferences", x => x.CustomerId);
                    table.ForeignKey(
                        name: "FK_CustomerAccommodationPreferences_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAddress",
                columns: table => new
                {
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Street = table.Column<string>(type: "text", nullable: false),
                    Complement = table.Column<string>(type: "text", nullable: true),
                    Neighborhood = table.Column<string>(type: "text", nullable: false),
                    PostalCode = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAddress", x => x.CustomerId);
                    table.ForeignKey(
                        name: "FK_CustomerAddress_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerContactInfo",
                columns: table => new
                {
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Mobile = table.Column<string>(type: "text", nullable: false),
                    Instagram = table.Column<string>(type: "text", nullable: true),
                    Facebook = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerContactInfo", x => x.CustomerId);
                    table.ForeignKey(
                        name: "FK_CustomerContactInfo_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerEmergencyContact",
                columns: table => new
                {
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Mobile = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerEmergencyContact", x => x.CustomerId);
                    table.ForeignKey(
                        name: "FK_CustomerEmergencyContact_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerIdentificationInfo",
                columns: table => new
                {
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    NationalId = table.Column<string>(type: "text", nullable: false),
                    IdNationality = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerIdentificationInfo", x => x.CustomerId);
                    table.ForeignKey(
                        name: "FK_CustomerIdentificationInfo_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerMedicalInfo",
                columns: table => new
                {
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Allergies = table.Column<string>(type: "text", nullable: true),
                    AdditionalInfo = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerMedicalInfo", x => x.CustomerId);
                    table.ForeignKey(
                        name: "FK_CustomerMedicalInfo_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPersonalInfo",
                columns: table => new
                {
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Gender = table.Column<string>(type: "text", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Nationality = table.Column<string>(type: "text", nullable: false),
                    Occupation = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPersonalInfo", x => x.CustomerId);
                    table.ForeignKey(
                        name: "FK_CustomerPersonalInfo_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPhysicalInfo",
                columns: table => new
                {
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric", nullable: false),
                    HeightCentimeters = table.Column<int>(type: "integer", nullable: false),
                    BikeType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPhysicalInfo", x => x.CustomerId);
                    table.ForeignKey(
                        name: "FK_CustomerPhysicalInfo_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentLineageId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Audience = table.Column<string>(type: "text", nullable: false),
                    TemplateId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TemplateVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    SourceVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    BrandingVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BrandingName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BrandingLogoUri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    BrandingPrimaryColor = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BrandingAccentColor = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BrandingBackgroundColor = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BrandingTextColor = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BrandingHeadingFontFamily = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BrandingBodyFontFamily = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BrandingFooterText = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetentionExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinalizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinalizedArtifactName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ReplacesDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    VoidReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    FinalizedArtifactContent = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentDrafts_DocumentLineages_DocumentLineageId",
                        column: x => x.DocumentLineageId,
                        principalTable: "DocumentLineages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TourId = table.Column<Guid>(type: "uuid", nullable: false),
                    BasePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    RoomType = table.Column<string>(type: "text", nullable: false),
                    RoomAdditionalCost = table.Column<decimal>(type: "numeric", nullable: false),
                    PrincipalCustomer_CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrincipalCustomer_BikeType = table.Column<string>(type: "text", nullable: false),
                    PrincipalCustomer_BikePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    CompanionCustomer_CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanionCustomer_BikeType = table.Column<string>(type: "text", nullable: true),
                    CompanionCustomer_BikePrice = table.Column<decimal>(type: "numeric", nullable: true),
                    Discount_Type = table.Column<string>(type: "text", nullable: false),
                    Discount_Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount_Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    BookingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Booking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Booking_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentDraftFields",
                columns: table => new
                {
                    FieldId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DocumentDraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    PrivacyClassification = table.Column<string>(type: "text", nullable: false),
                    IsEditable = table.Column<bool>(type: "boolean", nullable: false),
                    StaffOverride = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentDraftFields", x => new { x.DocumentDraftId, x.FieldId });
                    table.ForeignKey(
                        name: "FK_DocumentDraftFields_DocumentDrafts_DocumentDraftId",
                        column: x => x.DocumentDraftId,
                        principalTable: "DocumentDrafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Method = table.Column<string>(type: "text", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payment_Booking_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Booking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Booking_TourId",
                table: "Booking",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAuditRecords_RetentionExpiresAt",
                table: "DocumentAuditRecords",
                column: "RetentionExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentDrafts_RetentionExpiresAt_Unfinalized",
                table: "DocumentDrafts",
                column: "RetentionExpiresAt",
                filter: "\"FinalizedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_DocumentDrafts_DocumentLineageId_Revision",
                table: "DocumentDrafts",
                columns: new[] { "DocumentLineageId", "Revision" },
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE "DocumentDrafts"
                ADD COLUMN "ActiveFinalized" boolean
                GENERATED ALWAYS AS (
                    CASE WHEN "Status" = 'Finalized' THEN TRUE ELSE NULL END
                ) STORED;

                ALTER TABLE "DocumentDrafts"
                ADD CONSTRAINT "UQ_DocumentDrafts_ActiveFinalizedLineage"
                UNIQUE ("DocumentLineageId", "ActiveFinalized")
                DEFERRABLE INITIALLY DEFERRED;
                """);

            migrationBuilder.CreateIndex(
                name: "UX_DocumentLineages_BookingId_Type",
                table: "DocumentLineages",
                columns: new[] { "BookingId", "Type" },
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
                name: "IX_Payment_BookingId",
                table: "Payment",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Tours_Identifier",
                table: "Tours",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tours_Name",
                table: "Tours",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transport_messages_ConsumerName_EventId",
                schema: "messaging",
                table: "transport_messages",
                columns: new[] { "ConsumerName", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transport_messages_ConsumerName_ProcessedAt_NextConsumeAtte~",
                schema: "messaging",
                table: "transport_messages",
                columns: new[] { "ConsumerName", "ProcessedAt", "NextConsumeAttemptAt", "ReceivedAt" });

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION public."EnforceDocumentDraftBookingEligibility"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    booking_status text;
                BEGIN
                    SELECT "Status"
                    INTO booking_status
                    FROM public."Booking"
                    WHERE "Id" = NEW."BookingId"
                    FOR SHARE;

                    IF NOT FOUND OR booking_status NOT IN ('Confirmed', 'Completed') THEN
                        RAISE EXCEPTION USING
                            ERRCODE = '23514',
                            CONSTRAINT = 'CK_DocumentDrafts_BookingEligibility',
                            MESSAGE = 'Document drafts require an accepted booking.';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER "TR_DocumentDrafts_EnforceBookingEligibility"
                BEFORE INSERT ON public."DocumentDrafts"
                FOR EACH ROW EXECUTE FUNCTION public."EnforceDocumentDraftBookingEligibility"();
                """);

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION public."PreventDocumentAuditRecordMutation"()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF TG_OP = 'TRUNCATE' THEN
                        RAISE EXCEPTION 'Document audit records cannot be truncated.';
                    END IF;

                    IF TG_OP = 'UPDATE' THEN
                        RAISE EXCEPTION 'Document audit records are immutable.';
                    END IF;

                    IF OLD."RetentionExpiresAt" > CURRENT_TIMESTAMP THEN
                        RAISE EXCEPTION 'Document audit records cannot be deleted before their retention period expires.';
                    END IF;

                    RETURN OLD;
                END;
                $$;

                CREATE TRIGGER "TR_DocumentAuditRecords_RejectMutation"
                BEFORE UPDATE OR DELETE ON "DocumentAuditRecords"
                FOR EACH ROW EXECUTE FUNCTION public."PreventDocumentAuditRecordMutation"();

                CREATE TRIGGER "TR_DocumentAuditRecords_RejectTruncate"
                BEFORE TRUNCATE ON "DocumentAuditRecords"
                FOR EACH STATEMENT EXECUTE FUNCTION public."PreventDocumentAuditRecordMutation"();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS "TR_DocumentDrafts_EnforceBookingEligibility" ON public."DocumentDrafts";
                DROP FUNCTION IF EXISTS public."EnforceDocumentDraftBookingEligibility"();
                """);

            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS "TR_DocumentAuditRecords_RejectTruncate" ON "DocumentAuditRecords";
                DROP TRIGGER IF EXISTS "TR_DocumentAuditRecords_RejectMutation" ON "DocumentAuditRecords";
                DROP FUNCTION IF EXISTS public."PreventDocumentAuditRecordMutation"();
                """);

            migrationBuilder.DropTable(
                name: "CustomerAccommodationPreferences");

            migrationBuilder.DropTable(
                name: "CustomerAddress");

            migrationBuilder.DropTable(
                name: "CustomerContactInfo");

            migrationBuilder.DropTable(
                name: "CustomerEmergencyContact");

            migrationBuilder.DropTable(
                name: "CustomerIdentificationInfo");

            migrationBuilder.DropTable(
                name: "CustomerMedicalInfo");

            migrationBuilder.DropTable(
                name: "CustomerPersonalInfo");

            migrationBuilder.DropTable(
                name: "CustomerPhysicalInfo");

            migrationBuilder.DropTable(
                name: "DocumentAuditRecords");

            migrationBuilder.DropTable(
                name: "DocumentDraftFields");

            migrationBuilder.DropTable(
                name: "idempotency_keys",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "transport_messages",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "DocumentDrafts");

            migrationBuilder.DropTable(
                name: "Booking");

            migrationBuilder.DropTable(
                name: "DocumentLineages");

            migrationBuilder.DropTable(
                name: "Tours");
        }
    }
}
