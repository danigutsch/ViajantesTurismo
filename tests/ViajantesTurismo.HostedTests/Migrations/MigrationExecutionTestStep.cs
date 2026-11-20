namespace ViajantesTurismo.HostedTests.Migrations;

/// <summary>Describes one synthetic script in a test migration.</summary>
internal sealed record MigrationExecutionTestStep(
    string Phase,
    string Sql,
    string Transaction = "atomic",
    int TimeoutSeconds = 60,
    int LockTimeoutSeconds = 30,
    string VerifySql = "SELECT true;");
