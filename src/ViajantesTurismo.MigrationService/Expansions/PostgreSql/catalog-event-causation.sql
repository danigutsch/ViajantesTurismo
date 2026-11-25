ALTER TABLE "event_sourcing".events ADD COLUMN IF NOT EXISTS causation_event_id uuid;
