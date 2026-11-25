CREATE SCHEMA IF NOT EXISTS "document_maintenance";

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

CREATE TABLE document_maintenance.control_audit (
    "RequestId" uuid NOT NULL,
    "Job" text NOT NULL,
    "Action" text NOT NULL,
    "FailedRunId" uuid,
    "ActorId" character varying(128) NOT NULL,
    "Reason" text NOT NULL,
    "OccurredAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_control_audit" PRIMARY KEY ("RequestId")
);

INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261125144937_AddDocumentMaintenanceScheduler', '10.0.15');
