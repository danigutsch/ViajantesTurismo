WITH expected(table_name, index_name, column_names, descending) AS (
    VALUES
        ('outbox_messages', 'IX_outbox_messages_WorkflowCorrelationId_EnqueuedAt_Id',
            ARRAY['WorkflowCorrelationId', 'EnqueuedAt', 'Id'], ARRAY[false, true, true])
)
SELECT count(*) = 1
    AND EXISTS (
        SELECT 1
        FROM public."__EFMigrationsHistory_Branding"
        WHERE "MigrationId" = '20261126125357_AddBrandingWorkflowCorrelation'
    )
FROM expected
JOIN pg_catalog.pg_namespace AS relation_schema ON relation_schema.nspname = 'branding'
JOIN pg_catalog.pg_class AS relation
    ON relation.relnamespace = relation_schema.oid AND relation.relname = expected.table_name
JOIN pg_catalog.pg_attribute AS workflow_column
    ON workflow_column.attrelid = relation.oid AND workflow_column.attname = 'WorkflowCorrelationId'
JOIN pg_catalog.pg_class AS index_relation
    ON index_relation.relnamespace = relation_schema.oid AND index_relation.relname = expected.index_name
JOIN pg_catalog.pg_index AS definition
    ON definition.indexrelid = index_relation.oid AND definition.indrelid = relation.oid
JOIN pg_catalog.pg_am AS access_method ON access_method.oid = index_relation.relam
WHERE relation.relkind = 'r'
    AND workflow_column.attnum > 0
    AND NOT workflow_column.attisdropped
    AND workflow_column.atttypid = 'pg_catalog.uuid'::pg_catalog.regtype
    AND NOT workflow_column.attnotnull
    AND NOT workflow_column.atthasdef
    AND access_method.amname = 'btree'
    AND definition.indisvalid AND definition.indisready AND definition.indislive
    AND NOT definition.indisunique
    AND definition.indpred IS NULL AND definition.indexprs IS NULL
    AND definition.indnatts = pg_catalog.cardinality(expected.column_names)
    AND definition.indnkeyatts = definition.indnatts
    AND NOT EXISTS (
        SELECT 1
        FROM pg_catalog.unnest(definition.indkey) WITH ORDINALITY AS key_column(attribute_number, ordinal)
        LEFT JOIN pg_catalog.pg_attribute AS indexed_column
            ON indexed_column.attrelid = relation.oid AND indexed_column.attnum = key_column.attribute_number
        WHERE indexed_column.attname::text IS DISTINCT FROM expected.column_names[key_column.ordinal::integer]
            OR pg_catalog.pg_index_column_has_property(index_relation.oid, key_column.ordinal::integer, 'desc')
                IS DISTINCT FROM expected.descending[key_column.ordinal::integer]
            OR pg_catalog.pg_index_column_has_property(index_relation.oid, key_column.ordinal::integer, 'nulls_first')
                IS DISTINCT FROM expected.descending[key_column.ordinal::integer]
    );
