namespace ViajantesTurismo.HostedTests.Migrations;

/// <summary>Holds SQL for a disposable rehearsal of the Catalog public-content schema.</summary>
internal static class CatalogMigrationRehearsalArtifacts
{
    /// <summary>Restores the relevant InitialCatalog table shapes and synthetic published content.</summary>
    internal const string Snapshot = """
        CREATE TABLE public."__EFMigrationsHistory" (
            "MigrationId" varchar(150) PRIMARY KEY,
            "ProductVersion" varchar(32) NOT NULL
        );
        INSERT INTO public."__EFMigrationsHistory" VALUES ('20261111091535_InitialCatalog', '10.0.13');
        CREATE TABLE public."PublicContent" (
            "Id" uuid PRIMARY KEY,
            "Key" varchar(64) NOT NULL UNIQUE,
            "SourceLanguage" varchar(35) NOT NULL,
            "PublicationState" text NOT NULL
        );
        CREATE TABLE public."PublicContentVariants" (
            "PublicContentId" uuid NOT NULL REFERENCES public."PublicContent" ("Id") ON DELETE CASCADE,
            "Language" varchar(35) NOT NULL,
            "Title" varchar(128) NOT NULL,
            "Body" varchar(4000) NOT NULL,
            "SeoTitle" varchar(128),
            "MetaDescription" varchar(256),
            "ShareSummary" varchar(256),
            "RequiresHumanReview" boolean NOT NULL,
            PRIMARY KEY ("PublicContentId", "Language")
        );
        INSERT INTO public."PublicContent" VALUES ('55fbf552-6c0c-4238-a456-5627080252a1', 'HOME.REHEARSAL', 'en-US', 'Published');
        INSERT INTO public."PublicContentVariants" ("PublicContentId", "Language", "Title", "Body", "RequiresHumanReview") VALUES
            ('55fbf552-6c0c-4238-a456-5627080252a1', 'en-US', 'Rehearsal home', 'Synthetic English content', false),
            ('55fbf552-6c0c-4238-a456-5627080252a1', 'pt-BR', 'Página de ensaio', 'Conteúdo sintético', false);
        """;

    /// <summary>The safe reverse operation for the additive sample.</summary>
    internal const string RestorePreviousSchema = "ALTER TABLE public.\"PublicContent\" DROP COLUMN \"RehearsalRevision\";";

    /// <summary>A deliberately incorrect recovery script used to exercise the published-content gate.</summary>
    internal const string DropPortugueseVariant = RestorePreviousSchema + """

        DELETE FROM public."PublicContentVariants" WHERE "Language" = 'pt-BR';
        """;

    /// <summary>The declared recovery check for the restored public content.</summary>
    internal const string VerifyRecovery = """
        SELECT pg_catalog.current_setting('transaction_read_only') = 'on'
            AND EXISTS (SELECT 1 FROM public."PublicContent" WHERE "Key" = 'HOME.REHEARSAL'
                AND "Id" = '55fbf552-6c0c-4238-a456-5627080252a1' AND "PublicationState" = 'Published')
            AND EXISTS (SELECT 1 FROM public."PublicContentVariants" WHERE "Language" = 'en-US'
                AND "PublicContentId" = '55fbf552-6c0c-4238-a456-5627080252a1' AND NOT "RequiresHumanReview")
            AND EXISTS (SELECT 1 FROM public."PublicContentVariants" WHERE "Language" = 'pt-BR'
                AND "PublicContentId" = '55fbf552-6c0c-4238-a456-5627080252a1' AND NOT "RequiresHumanReview")
            AND NOT EXISTS (SELECT 1 FROM pg_catalog.pg_attribute
                WHERE attrelid = 'public."PublicContent"'::regclass AND attname = 'RehearsalRevision' AND NOT attisdropped);
        """;

    /// <summary>The additive schema, bounded backfill, and scoped verification for the rehearsal.</summary>
    internal static IReadOnlyList<MigrationExecutionTestStep> Upgrade =>
    [
        new("expand", "ALTER TABLE public.\"PublicContent\" ADD COLUMN \"RehearsalRevision\" integer;", TimeoutSeconds: 30, LockTimeoutSeconds: 5),
        new("data", """
            WITH changed AS (
                UPDATE public."PublicContent" SET "RehearsalRevision" = COALESCE("RehearsalRevision", 0) + 1
                WHERE "Key" = 'HOME.REHEARSAL' AND $1 = 0 AND $2 > 0 RETURNING 1
            )
            SELECT CASE WHEN count(*) = 1 THEN 1::bigint ELSE $1 END, count(*)::integer, count(*) = 0 FROM changed;
            """, "batched", TimeoutSeconds: 30, LockTimeoutSeconds: 5, VerifySql: """
                SELECT ready AND EXISTS (SELECT 1 FROM public."PublicContent"
                    WHERE "Key" = 'HOME.REHEARSAL' AND "RehearsalRevision" = 1)
                FROM migration_fixture.gate;
                """),
        new("verify", """
            SELECT count(*) = 2 AND pg_catalog.current_setting('transaction_read_only') = 'on'
            FROM public."PublicContentVariants" WHERE "PublicContentId" = '55fbf552-6c0c-4238-a456-5627080252a1';
            """, TimeoutSeconds: 30, LockTimeoutSeconds: 5)
    ];
}
