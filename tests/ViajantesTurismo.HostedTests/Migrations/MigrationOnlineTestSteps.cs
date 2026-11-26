namespace ViajantesTurismo.HostedTests.Migrations;

/// <summary>Provides synthetic concurrent index declarations for standalone test cases.</summary>
internal static class MigrationOnlineTestSteps
{
    private const string GalleryOutcomeHeader =
        """CREATE INDEX CONCURRENTLY "ix_catalog_events_gallery_intent_outcome" ON "event_sourcing"."events" USING btree """;
    private const string GalleryOutcomeKeys =
        """("stream_id", ("payload_json" ->> 'IntentId'), ("payload_json" ->> 'IntentSequence'), "stream_revision")""";
    private const string GalleryOutcomeInclude = """ INCLUDE ("event_type", "recorded_at_utc")""";
    private const string GalleryOutcomePredicate =
        """ WHERE "event_type" IN ('catalog.tours.gallery-changed.v1', 'catalog.tours.gallery-intent-rejected.v1');""";

    /// <summary>The normal SQL artifact for the fixture's concurrent index.</summary>
    internal const string IndexSql = """CREATE INDEX CONCURRENTLY "online_entries_category" ON "migration_fixture"."online_entries" USING btree ("category");""";

    /// <summary>The reviewed Gallery outcome index for the provider event store.</summary>
    internal const string GalleryOutcomeSql = GalleryOutcomeHeader + GalleryOutcomeKeys + GalleryOutcomeInclude + GalleryOutcomePredicate;

    /// <summary>A same-name index using another JSON property.</summary>
    internal const string GalleryWrongKeySql = GalleryOutcomeHeader
        + """("stream_id", ("payload_json" ->> 'LegacyIntentId'), ("payload_json" ->> 'IntentSequence'), "stream_revision")"""
        + GalleryOutcomeInclude + GalleryOutcomePredicate;

    /// <summary>A same-name index omitting a stored column.</summary>
    internal const string GalleryWrongIncludeSql = GalleryOutcomeHeader + GalleryOutcomeKeys
        + """ INCLUDE ("recorded_at_utc")""" + GalleryOutcomePredicate;

    /// <summary>A same-name index matching a different outcome type.</summary>
    internal const string GalleryWrongPredicateSql = GalleryOutcomeHeader + GalleryOutcomeKeys + GalleryOutcomeInclude
        + """ WHERE "event_type" IN ('catalog.tours.legacy-gallery.v1', 'catalog.tours.gallery-intent-rejected.v1');""";

    /// <summary>Declares the two concurrent indexes required by the Gallery point query.</summary>
    internal static IReadOnlyList<MigrationExecutionTestStep> Gallery(string outcomeVerification) =>
    [
        new("expand", GalleryOutcomeSql, "online", VerifySql: outcomeVerification, OnlineIndex: new
        {
            schema = "event_sourcing",
            table = "events",
            name = "ix_catalog_events_gallery_intent_outcome",
            keys = new object[]
            {
                new { column = "stream_id" },
                new { column = "payload_json", jsonProperty = "IntentId" },
                new { column = "payload_json", jsonProperty = "IntentSequence" },
                new { column = "stream_revision" }
            },
            include = new[] { "event_type", "recorded_at_utc" },
            textFilter = new
            {
                column = "event_type",
                values = new[] { "catalog.tours.gallery-changed.v1", "catalog.tours.gallery-intent-rejected.v1" }
            },
            unique = false
        }),
        new("expand", """CREATE INDEX CONCURRENTLY "ix_catalog_events_tour_retirement" ON "event_sourcing"."events" USING btree """
            + """("stream_id", "stream_revision") INCLUDE ("recorded_at_utc") WHERE "event_type" = 'catalog.tours.retired.v1';""",
            "online", VerifySql: "SELECT true;", OnlineIndex: new
        {
            schema = "event_sourcing",
            table = "events",
            name = "ix_catalog_events_tour_retirement",
            columns = new[] { "stream_id", "stream_revision" },
            include = new[] { "recorded_at_utc" },
            textFilter = new { column = "event_type", values = new[] { "catalog.tours.retired.v1" } },
            unique = false
        }),
        new("verify", "SELECT true;")
    ];

    /// <summary>Creates an online expansion and the required separate verification phase.</summary>
    internal static IReadOnlyList<MigrationExecutionTestStep> Index(
        bool unique = false, int timeout = 60, int lockTimeout = 30, string verification = "SELECT true;", string extraSql = "") =>
    [
        new("expand", (unique ? IndexSql.Replace("CREATE INDEX", "CREATE UNIQUE INDEX", StringComparison.Ordinal) : IndexSql) + extraSql,
            "online", timeout, lockTimeout, verification,
            new { schema = "migration_fixture", table = "online_entries", name = "online_entries_category", columns = new[] { "category" }, unique }),
        new("verify", "SELECT true;")
    ];
}
