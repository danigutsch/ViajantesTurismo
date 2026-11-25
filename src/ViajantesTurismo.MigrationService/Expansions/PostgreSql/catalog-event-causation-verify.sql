SELECT EXISTS (
    SELECT 1
    FROM pg_catalog.pg_attribute AS attribute
    JOIN pg_catalog.pg_class AS relation ON relation.oid = attribute.attrelid
    WHERE relation.oid = pg_catalog.to_regclass('event_sourcing.events')
      AND relation.relkind IN ('r', 'p')
      AND attribute.attname = 'causation_event_id'
      AND attribute.atttypid = 'pg_catalog.uuid'::pg_catalog.regtype
      AND NOT attribute.attnotnull
      AND NOT attribute.atthasdef
      AND NOT attribute.attisdropped
);
