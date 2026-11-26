namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Declares the exact PostgreSQL B-tree index an online expansion may create.</summary>
/// <param name="Schema">The schema that owns the table and index.</param>
/// <param name="Table">The table to index.</param>
/// <param name="Name">The index name within the schema.</param>
/// <param name="Unique">Whether the index enforces uniqueness.</param>
/// <param name="Columns">Ordered plain B-tree key columns in the existing manifest format.</param>
/// <param name="Keys">Ordered typed keys used in place of plain columns.</param>
/// <param name="Include">Optional columns stored without participating in comparison.</param>
/// <param name="TextFilter">An optional equality or membership predicate on one text column.</param>
internal sealed record MigrationOnlineIndex(
    string Schema,
    string Table,
    string Name,
    bool Unique,
    string[]? Columns = null,
    MigrationOnlineIndexKey[]? Keys = null,
    string[]? Include = null,
    MigrationOnlineIndexTextFilter? TextFilter = null)
{
    /// <summary>Validates bounded, unambiguous keys, included columns, and text values.</summary>
    internal void Validate()
    {
        var keys = OrderedKeys();
        if (!IsIdentifier(Schema) || !IsIdentifier(Table) || !IsIdentifier(Name)
            || string.Equals(Name, Table, StringComparison.Ordinal)
            || (Columns is null) == (Keys is null)
            || keys.Length is < 1 or > 32
            || keys.Any(static key => key is null || !IsIdentifier(key.Column) || (key.JsonProperty is not null && !IsToken(key.JsonProperty)))
            || keys.Distinct().Count() != keys.Length
            || (Include is { } included && (included.Length == 0 || keys.Length + included.Length > 32
                || !included.All(IsIdentifier) || included.Distinct(StringComparer.Ordinal).Count() != included.Length
                || keys.Any(key => included.Contains(key.Column, StringComparer.Ordinal))))
            || (TextFilter is { } filter && (!IsIdentifier(filter.Column) || filter.Values is not { Length: >= 1 and <= 32 }
                || !filter.Values.All(IsToken) || filter.Values.Distinct(StringComparer.Ordinal).Count() != filter.Values.Length)))
        {
            throw new InvalidDataException("Invalid online index declaration.");
        }
    }

    /// <summary>Returns the validated keys in the order defined by either manifest form.</summary>
    internal MigrationOnlineIndexKey[] OrderedKeys() =>
        Keys ?? Columns?.Select(static column => new MigrationOnlineIndexKey(column)).ToArray() ?? [];

    /// <summary>Requires the reviewed bytes to encode the single declared index operation.</summary>
    internal void ValidateScript(string sql)
    {
        var kind = Unique ? "UNIQUE INDEX" : "INDEX";
        var keys = string.Join(", ", OrderedKeys().Select(static key => key.JsonProperty is { } property
            ? $"({Quote(key.Column)} ->> {Literal(property)})"
            : Quote(key.Column)));
        var included = Include is { } columns ? $" INCLUDE ({string.Join(", ", columns.Select(Quote))})" : string.Empty;
        var filter = TextFilter switch
        {
            { Values: [var value] } single => $" WHERE {Quote(single.Column)} = {Literal(value)}",
            { } multiple => $" WHERE {Quote(multiple.Column)} IN ({string.Join(", ", multiple.Values.Select(Literal))})",
            _ => string.Empty
        };
        var statement = $"CREATE {kind} CONCURRENTLY {Quote(Name)} ON {Quote(Schema)}.{Quote(Table)} USING btree ({keys}){included}{filter};";
        if (!string.Equals(sql, statement, StringComparison.Ordinal) && !string.Equals(sql, statement + "\n", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Online index script must match the declared single concurrent index creation.");
        }
    }

    private static bool IsIdentifier(string? value) =>
        value is { Length: > 0 and <= 63 } && (value[0] is >= 'a' and <= 'z' or '_')
        && value.All(static character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_');

    private static bool IsToken(string? value) =>
        value is { Length: > 0 and <= 128 }
        && value.All(static character => char.IsAsciiLetterOrDigit(character) || character is '_' or '.' or '-');

    private static string Quote(string identifier) => $"\"{identifier}\"";

    private static string Literal(string value) => $"'{value}'";
}
