SELECT count(*) = 3 AND pg_catalog.bool_and(relation.relkind = 'r')
FROM pg_catalog.pg_class AS relation
JOIN pg_catalog.pg_namespace AS namespace ON namespace.oid = relation.relnamespace
WHERE namespace.nspname = 'document_maintenance'
  AND relation.relname IN ('scheduler_jobs', 'scheduler_executions', 'control_audit');
