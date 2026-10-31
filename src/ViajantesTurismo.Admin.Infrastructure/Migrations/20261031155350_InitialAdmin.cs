using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                name: "disposition_hold");

            migrationBuilder.EnsureSchema(
                name: "messaging");

            migrationBuilder.EnsureSchema(
                name: "branding_projection");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF (SELECT datctype FROM pg_database WHERE datname = current_database())
                        NOT IN ('C.UTF-8', 'C.utf8', 'en_US.UTF-8', 'en_US.utf8') THEN
                        RAISE EXCEPTION 'Unsupported PostgreSQL LC_CTYPE for customer email uniqueness.';
                    END IF;
                END;
                $$;
                """);

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
                name: "DocumentAuditPurgeRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PurgedCount = table.Column<int>(type: "integer", nullable: false),
                    HeldCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    RetentionCutoffUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAuditPurgeRecords", x => x.Id);
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
                    HoldId = table.Column<Guid>(type: "uuid", nullable: true),
                    HoldBasis = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RetentionExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DocumentLineageId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAuditRecords", x => x.Id);
                    table.CheckConstraint("CK_DocumentAuditRecords_DocumentLineageStamped", "\"DocumentId\" IS NULL OR \"DocumentLineageId\" IS NOT NULL");
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
                name: "DocumentRetentionPurgeRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    PurgedCount = table.Column<int>(type: "integer", nullable: false),
                    HeldCount = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentRetentionPurgeRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "holds",
                schema: "disposition_hold",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<string>(type: "character varying(193)", maxLength: 193, nullable: false),
                    Basis = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    PurposeCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AuthorityReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PlacedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PlacedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewDueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReleasedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReleaseReasonCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_holds", x => x.Id);
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
                name: "inbox_messages",
                schema: "branding_projection",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEpoch = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceRevision = table.Column<long>(type: "bigint", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ReasonCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => x.EventId);
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
                name: "PaymentAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    ProtectedState = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    ActorId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Operation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SelfApproved = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "recovery",
                schema: "branding_projection",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Since = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AuthorizedEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorizedEpoch = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorizedRevision = table.Column<long>(type: "bigint", nullable: true),
                    AuthorizationReason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IncidentReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AuthorizedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AuthorizedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recovery", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "recovery_audit",
                schema: "branding_projection",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OldEpoch = table.Column<Guid>(type: "uuid", nullable: true),
                    OldRevision = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_recovery_audit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "settings",
                schema: "branding_projection",
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
                    LogoSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LogoContentType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LogoContent = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Identifier = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
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
                    PoisonedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    Email = table.Column<string>(type: "citext", nullable: false),
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
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: false),
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
                    BrandingSourceRevision = table.Column<long>(type: "bigint", nullable: true),
                    BrandingName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BrandingLogoUri = table.Column<string>(type: "character varying(60000)", maxLength: 60000, nullable: true),
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
                    VoidReason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
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
                    Value = table.Column<string>(type: "text", nullable: false),
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
                name: "BookingPaymentHistories",
                columns: table => new
                {
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    ProtectedState = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingPaymentHistories", x => x.BookingId);
                    table.ForeignKey(
                        name: "FK_BookingPaymentHistories_Booking_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Booking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentInstructionSnapshots",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    ProtectedSnapshot = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentInstructionSnapshots", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_PaymentInstructionSnapshots_Booking_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Booking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentInstructionSnapshots_DocumentDrafts_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "DocumentDrafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentInstructionSnapshots_PaymentAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "PaymentAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Booking_TourId",
                table: "Booking",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "UX_CustomerContactInfo_Email",
                table: "CustomerContactInfo",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAuditRecords_DocumentLineageId",
                table: "DocumentAuditRecords",
                column: "DocumentLineageId");

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
                name: "UX_DocumentDrafts_BookingId_Type_Revision",
                table: "DocumentDrafts",
                columns: new[] { "BookingId", "Type", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_DocumentDrafts_DocumentLineageId_Revision",
                table: "DocumentDrafts",
                columns: new[] { "DocumentLineageId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_DocumentLineages_BookingId_Type",
                table: "DocumentLineages",
                columns: new[] { "BookingId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_holds_Scope",
                schema: "disposition_hold",
                table: "holds",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_Outcome",
                schema: "branding_projection",
                table: "inbox_messages",
                column: "Outcome");

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
                name: "IX_PaymentAudits_ResourceId_OccurredAt",
                table: "PaymentAudits",
                columns: new[] { "ResourceId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAudits_Sequence",
                table: "PaymentAudits",
                column: "Sequence",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentInstructionSnapshots_AccountId",
                table: "PaymentInstructionSnapshots",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentInstructionSnapshots_BookingId",
                table: "PaymentInstructionSnapshots",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_recovery_audit_Kind_OldEpoch",
                schema: "branding_projection",
                table: "recovery_audit",
                columns: new[] { "Kind", "OldEpoch" });

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
                name: "IX_transport_messages_ConsumerName_PoisonedAt",
                schema: "messaging",
                table: "transport_messages",
                columns: new[] { "ConsumerName", "PoisonedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_transport_messages_ConsumerName_ProcessedAt_NextConsumeAtte~",
                schema: "messaging",
                table: "transport_messages",
                columns: new[] { "ConsumerName", "ProcessedAt", "NextConsumeAttemptAt", "ReceivedAt" });

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

            migrationBuilder.Sql(
                """
                CREATE FUNCTION public."EnforceDocumentDraftBookingEligibility"()
                RETURNS trigger LANGUAGE plpgsql AS $$
                DECLARE booking_status text;
                BEGIN
                    SELECT "Status" INTO booking_status FROM public."Booking" WHERE "Id" = NEW."BookingId" FOR SHARE;
                    IF NOT FOUND OR
                        (NEW."Type" = 'PaymentInstructions' AND booking_status NOT IN ('Pending', 'Confirmed', 'Completed')) OR
                        (NEW."Type" <> 'PaymentInstructions' AND booking_status NOT IN ('Confirmed', 'Completed')) THEN
                        RAISE EXCEPTION USING ERRCODE = '23514', CONSTRAINT = 'CK_DocumentDrafts_BookingEligibility', MESSAGE = 'The document booking is ineligible.';
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
                CREATE FUNCTION public."PreventDocumentAuditRecordMutation"()
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

            migrationBuilder.Sql(
                """
                CREATE FUNCTION public."RejectPaymentHistoryMutation"()
                RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION USING ERRCODE = '23514', MESSAGE = 'Payment records and audit evidence are append-only.';
                END;
                $$;
                CREATE TRIGGER "TR_PaymentAudits_Immutable" BEFORE UPDATE OR DELETE ON "PaymentAudits"
                    FOR EACH ROW EXECUTE FUNCTION public."RejectPaymentHistoryMutation"();
                CREATE TRIGGER "TR_PaymentInstructions_Immutable" BEFORE UPDATE ON "PaymentInstructionSnapshots"
                    FOR EACH ROW EXECUTE FUNCTION public."RejectPaymentHistoryMutation"();

                CREATE FUNCTION public."EnforcePaymentVersionAdvance"()
                RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF NEW."Version" <= OLD."Version" THEN
                        RAISE EXCEPTION USING ERRCODE = '23514', MESSAGE = 'Payment record versions must advance.';
                    END IF;
                    RETURN NEW;
                END;
                $$;
                CREATE TRIGGER "TR_PaymentAccounts_Version" BEFORE UPDATE ON "PaymentAccounts"
                    FOR EACH ROW EXECUTE FUNCTION public."EnforcePaymentVersionAdvance"();
                CREATE TRIGGER "TR_BookingPaymentHistories_Version" BEFORE UPDATE ON "BookingPaymentHistories"
                    FOR EACH ROW EXECUTE FUNCTION public."EnforcePaymentVersionAdvance"();
                """);
        }

        /// <inheritdoc />
        /// <remarks>A rollback must never erase financial history.</remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM "PaymentAccounts") OR EXISTS (SELECT 1 FROM "BookingPaymentHistories")
                        OR EXISTS (SELECT 1 FROM "PaymentAudits") OR EXISTS (SELECT 1 FROM "PaymentInstructionSnapshots") THEN
                        RAISE EXCEPTION 'Payment history exists. Use an approved forward recovery or restore procedure.';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                DROP TRIGGER "TR_BookingPaymentHistories_Version" ON "BookingPaymentHistories";
                DROP TRIGGER "TR_PaymentAccounts_Version" ON "PaymentAccounts";
                DROP TRIGGER "TR_PaymentInstructions_Immutable" ON "PaymentInstructionSnapshots";
                DROP TRIGGER "TR_PaymentAudits_Immutable" ON "PaymentAudits";
                DROP FUNCTION public."EnforcePaymentVersionAdvance"();
                DROP FUNCTION public."RejectPaymentHistoryMutation"();
                DROP TRIGGER "TR_DocumentAuditRecords_RejectTruncate" ON "DocumentAuditRecords";
                DROP TRIGGER "TR_DocumentAuditRecords_RejectMutation" ON "DocumentAuditRecords";
                DROP FUNCTION public."PreventDocumentAuditRecordMutation"();
                DROP TRIGGER "TR_DocumentDrafts_EnforceBookingEligibility" ON public."DocumentDrafts";
                DROP FUNCTION public."EnforceDocumentDraftBookingEligibility"();
                """);

            migrationBuilder.DropTable(
                name: "BookingPaymentHistories");

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
                name: "DocumentAuditPurgeRecords");

            migrationBuilder.DropTable(
                name: "DocumentAuditRecords");

            migrationBuilder.DropTable(
                name: "DocumentDraftFields");

            migrationBuilder.DropTable(
                name: "holds",
                schema: "disposition_hold");

            migrationBuilder.DropTable(
                name: "idempotency_keys",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "branding_projection");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "PaymentAudits");

            migrationBuilder.DropTable(
                name: "PaymentInstructionSnapshots");

            migrationBuilder.DropTable(
                name: "recovery",
                schema: "branding_projection");

            migrationBuilder.DropTable(
                name: "recovery_audit",
                schema: "branding_projection");

            migrationBuilder.DropTable(
                name: "settings",
                schema: "branding_projection");

            migrationBuilder.DropTable(
                name: "transport_messages",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Booking");

            migrationBuilder.DropTable(
                name: "DocumentDrafts");

            migrationBuilder.DropTable(
                name: "PaymentAccounts");

            migrationBuilder.DropTable(
                name: "Tours");

            migrationBuilder.DropTable(
                name: "DocumentLineages");

            migrationBuilder.DropTable(
                name: "DocumentRetentionPurgeRecords");
        }
    }
}
