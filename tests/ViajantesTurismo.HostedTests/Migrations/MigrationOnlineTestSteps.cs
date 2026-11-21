namespace ViajantesTurismo.HostedTests.Migrations;

/// <summary>Provides synthetic concurrent index declarations for standalone test cases.</summary>
internal static class MigrationOnlineTestSteps
{
    /// <summary>The normal SQL artifact for the fixture's concurrent index.</summary>
    internal const string IndexSql = """CREATE INDEX CONCURRENTLY "online_entries_category" ON "migration_fixture"."online_entries" USING btree ("category");""";

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
