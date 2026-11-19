-- In psql, set after_revision_id to the last revision from the prior page.
-- Start with the nil UUID. This query reads at most 100 finalized revisions.
WITH revisions AS MATERIALIZED (
    SELECT "Id", "FinalizedArtifactLength", "FinalizedArtifactSha256"
    FROM "DocumentDrafts"
    WHERE "FinalizedAt" IS NOT NULL
      AND "Id" > CAST(:'after_revision_id' AS uuid)
    ORDER BY "Id"
    LIMIT 100
)
SELECT revision."Id" AS "RevisionId",
    CASE
        WHEN body."DocumentId" IS NULL THEN 'missing_body'
        WHEN revision."FinalizedArtifactLength" IS NULL OR revision."FinalizedArtifactSha256" IS NULL
            THEN 'missing_integrity_metadata'
        WHEN revision."FinalizedArtifactLength" <= 0
            OR revision."FinalizedArtifactLength" <> octet_length(body."Content")
            THEN 'length_mismatch'
        WHEN (revision."FinalizedArtifactSha256" COLLATE "C") <> encode(sha256(body."Content"), 'hex')
            THEN 'digest_mismatch'
        ELSE 'intact'
    END AS "Integrity"
FROM revisions AS revision
LEFT JOIN "DocumentArtifactBodies" AS body ON body."DocumentId" = revision."Id"
ORDER BY revision."Id";
