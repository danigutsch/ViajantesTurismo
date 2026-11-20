namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Declares the exact ordinary PostgreSQL index an online expansion may create.</summary>
/// <param name="Schema">The schema that owns the table and index.</param>
/// <param name="Table">The table to index.</param>
/// <param name="Name">The index name within the schema.</param>
/// <param name="Columns">Ordered B-tree key columns.</param>
/// <param name="Unique">Whether the index enforces uniqueness.</param>
internal sealed record MigrationOnlineIndex(string Schema, string Table, string Name, string[] Columns, bool Unique)
{
    /// <summary>Validates a bounded list of unambiguous PostgreSQL identifiers.</summary>
    internal void Validate()
    {
        if (!IsIdentifier(Schema) || !IsIdentifier(Table) || !IsIdentifier(Name)
            || string.Equals(Name, Table, StringComparison.Ordinal)
            || Columns.Length is < 1 or > 32 || !Columns.All(IsIdentifier)
            || Columns.Distinct(StringComparer.Ordinal).Count() != Columns.Length)
        {
            throw new InvalidDataException("Invalid online index declaration.");
        }
    }

    /// <summary>Requires the reviewed bytes to encode the single declared index operation.</summary>
    internal void ValidateScript(string sql)
    {
        var kind = Unique ? "UNIQUE INDEX" : "INDEX";
        var keys = string.Join(", ", Columns.Select(Quote));
        var statement = $"CREATE {kind} CONCURRENTLY {Quote(Name)} ON {Quote(Schema)}.{Quote(Table)} USING btree ({keys});";
        if (!string.Equals(sql, statement, StringComparison.Ordinal) && !string.Equals(sql, statement + "\n", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Online index script must match the declared single concurrent index creation.");
        }
    }

    private static bool IsIdentifier(string? value) =>
        value is { Length: > 0 and <= 63 } && (value[0] is >= 'a' and <= 'z' or '_')
        && value.All(static character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_');

    private static string Quote(string identifier) => $"\"{identifier}\"";
}
