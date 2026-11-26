using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentMaintenanceScheduler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "document_maintenance");

            // The provider owns the scheduler relations outside the EF model.
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "document_maintenance".scheduler_jobs (
                    job_name text COLLATE pg_catalog."C" PRIMARY KEY,
                    next_due_at timestamptz NULL,
                    fixed_delay_microseconds bigint NULL CHECK (fixed_delay_microseconds > 0),
                    paused boolean NOT NULL DEFAULT false,
                    CHECK (pg_catalog.length(job_name) BETWEEN 1 AND 128)
                );
                CREATE TABLE IF NOT EXISTS "document_maintenance".scheduler_executions (
                    run_id uuid PRIMARY KEY,
                    job_name text COLLATE pg_catalog."C" NOT NULL REFERENCES "document_maintenance".scheduler_jobs(job_name),
                    kind integer NOT NULL CHECK (kind IN (0, 1, 2)),
                    available_at timestamptz NOT NULL,
                    scheduled_for timestamptz NULL,
                    retry_of uuid NULL,
                    started_at timestamptz NULL,
                    observed_until timestamptz NULL,
                    lease_fence uuid NULL,
                    finished_at timestamptz NULL,
                    outcome integer NULL CHECK (outcome IN (2, 3, 4, 5, 6)),
                    retry_at timestamptz NULL,
                    CHECK ((kind = 0) = (scheduled_for IS NOT NULL)),
                    CHECK ((kind = 2) = (retry_of IS NOT NULL)),
                    CHECK ((started_at IS NULL) = (observed_until IS NULL)),
                    CHECK ((started_at IS NOT NULL AND outcome IS NULL) = (lease_fence IS NOT NULL)),
                    CHECK ((finished_at IS NULL) = (outcome IS NULL)),
                    CHECK (finished_at IS NULL OR started_at IS NOT NULL),
                    CHECK (COALESCE(outcome = 5, false) = (retry_at IS NOT NULL))
                );
                CREATE UNIQUE INDEX IF NOT EXISTS scheduler_executions_running_job
                    ON "document_maintenance".scheduler_executions(job_name) WHERE started_at IS NOT NULL AND outcome IS NULL;
                CREATE INDEX IF NOT EXISTS scheduler_executions_ready
                    ON "document_maintenance".scheduler_executions(job_name, (started_at IS NOT NULL) DESC, available_at, run_id) WHERE outcome IS NULL;
                CREATE INDEX IF NOT EXISTS scheduler_executions_history
                    ON "document_maintenance".scheduler_executions(job_name, started_at DESC, run_id DESC) WHERE started_at IS NOT NULL;
                CREATE INDEX IF NOT EXISTS scheduler_jobs_due
                    ON "document_maintenance".scheduler_jobs(next_due_at, job_name) WHERE NOT paused;
                """);

            migrationBuilder.CreateTable(
                name: "control_audit",
                schema: "document_maintenance",
                columns: table => new
                {
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Job = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    FailedRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_audit", x => x.RequestId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE "document_maintenance".scheduler_executions;
                DROP TABLE "document_maintenance".scheduler_jobs;
                """);

            migrationBuilder.DropTable(
                name: "control_audit",
                schema: "document_maintenance");
        }
    }
}
