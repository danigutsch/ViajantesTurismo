using System;
using Microsoft.EntityFrameworkCore.Migrations;
using ViajantesTurismo.Admin.Infrastructure.Payments;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PaymentLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

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

            migrationBuilder.Sql(
                $"""
                CREATE OR REPLACE FUNCTION {PaymentSchema.DefaultSchema}."EnforceDocumentDraftBookingEligibility"()
                RETURNS trigger LANGUAGE plpgsql AS $$
                DECLARE booking_status text;
                BEGIN
                    SELECT "Status" INTO booking_status FROM {PaymentSchema.DefaultSchema}."Booking" WHERE "Id" = NEW."BookingId" FOR SHARE;
                    IF NOT FOUND OR
                        (NEW."Type" = 'PaymentInstructions' AND booking_status NOT IN ('Pending', 'Confirmed', 'Completed')) OR
                        (NEW."Type" <> 'PaymentInstructions' AND booking_status NOT IN ('Confirmed', 'Completed')) THEN
                        RAISE EXCEPTION USING ERRCODE = '23514', CONSTRAINT = 'CK_DocumentDrafts_BookingEligibility', MESSAGE = 'The document booking is ineligible.';
                    END IF;
                    RETURN NEW;
                END;
                $$;

                CREATE FUNCTION {PaymentSchema.DefaultSchema}."RejectPaymentHistoryMutation"()
                RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION USING ERRCODE = '23514', MESSAGE = 'Payment records and audit evidence are append-only.';
                END;
                $$;
                CREATE TRIGGER "TR_PaymentAudits_Immutable" BEFORE UPDATE OR DELETE ON "PaymentAudits"
                    FOR EACH ROW EXECUTE FUNCTION {PaymentSchema.DefaultSchema}."RejectPaymentHistoryMutation"();
                CREATE TRIGGER "TR_PaymentInstructions_Immutable" BEFORE UPDATE ON "PaymentInstructionSnapshots"
                    FOR EACH ROW EXECUTE FUNCTION {PaymentSchema.DefaultSchema}."RejectPaymentHistoryMutation"();

                CREATE FUNCTION {PaymentSchema.DefaultSchema}."EnforcePaymentVersionAdvance"()
                RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF NEW."Version" <= OLD."Version" THEN
                        RAISE EXCEPTION USING ERRCODE = '23514', MESSAGE = 'Payment record versions must advance.';
                    END IF;
                    RETURN NEW;
                END;
                $$;
                CREATE TRIGGER "TR_PaymentAccounts_Version" BEFORE UPDATE ON "PaymentAccounts"
                    FOR EACH ROW EXECUTE FUNCTION {PaymentSchema.DefaultSchema}."EnforcePaymentVersionAdvance"();
                CREATE TRIGGER "TR_BookingPaymentHistories_Version" BEFORE UPDATE ON "BookingPaymentHistories"
                    FOR EACH ROW EXECUTE FUNCTION {PaymentSchema.DefaultSchema}."EnforcePaymentVersionAdvance"();
                """);
        }

        /// <inheritdoc />
        /// <remarks>A rollback must never erase new financial history or mislabel cross-currency receipts.</remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM "PaymentAccounts") OR EXISTS (SELECT 1 FROM "BookingPaymentHistories")
                        OR EXISTS (SELECT 1 FROM "PaymentAudits") OR EXISTS (SELECT 1 FROM "PaymentInstructionSnapshots") THEN
                        RAISE EXCEPTION 'Payment history exists. Use an approved forward recovery or restore procedure.';
                    END IF;
                END $$;
                DROP TRIGGER "TR_PaymentAudits_Immutable" ON "PaymentAudits";
                DROP TRIGGER "TR_PaymentInstructions_Immutable" ON "PaymentInstructionSnapshots";
                DROP TRIGGER "TR_PaymentAccounts_Version" ON "PaymentAccounts";
                DROP TRIGGER "TR_BookingPaymentHistories_Version" ON "BookingPaymentHistories";
                DROP FUNCTION {PaymentSchema.DefaultSchema}."RejectPaymentHistoryMutation"();
                DROP FUNCTION {PaymentSchema.DefaultSchema}."EnforcePaymentVersionAdvance"();
                CREATE OR REPLACE FUNCTION {PaymentSchema.DefaultSchema}."EnforceDocumentDraftBookingEligibility"()
                RETURNS trigger LANGUAGE plpgsql AS $$
                DECLARE booking_status text;
                BEGIN
                    SELECT "Status" INTO booking_status FROM {PaymentSchema.DefaultSchema}."Booking" WHERE "Id" = NEW."BookingId" FOR SHARE;
                    IF NOT FOUND OR booking_status NOT IN ('Confirmed', 'Completed') THEN
                        RAISE EXCEPTION USING ERRCODE = '23514', CONSTRAINT = 'CK_DocumentDrafts_BookingEligibility', MESSAGE = 'Document drafts require an accepted booking.';
                    END IF;
                    RETURN NEW;
                END;
                $$;
                """);

            migrationBuilder.DropTable(
                name: "BookingPaymentHistories");

            migrationBuilder.DropTable(
                name: "PaymentAudits");

            migrationBuilder.DropTable(
                name: "PaymentInstructionSnapshots");

            migrationBuilder.DropTable(
                name: "PaymentAccounts");

        }
    }
}
