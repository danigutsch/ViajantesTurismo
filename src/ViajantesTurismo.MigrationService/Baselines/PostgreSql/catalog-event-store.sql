CREATE SCHEMA IF NOT EXISTS "event_sourcing";

CREATE TABLE IF NOT EXISTS "event_sourcing".event_streams (
    stream_id text PRIMARY KEY,
    stream_revision bigint NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL
);

CREATE TABLE IF NOT EXISTS "event_sourcing".events (
    position bigint GENERATED ALWAYS AS IDENTITY UNIQUE,
    event_id uuid PRIMARY KEY,
    stream_id text NOT NULL REFERENCES "event_sourcing".event_streams(stream_id) ON DELETE RESTRICT,
    stream_revision bigint NOT NULL,
    event_type text NOT NULL,
    payload_json jsonb NOT NULL,
    recorded_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT uq_events_stream_revision UNIQUE (stream_id, stream_revision)
);

CREATE TABLE IF NOT EXISTS "event_sourcing".projection_checkpoints (
    projection_name text PRIMARY KEY,
    position bigint NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL
);
