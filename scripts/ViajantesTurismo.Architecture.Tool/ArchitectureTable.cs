namespace ViajantesTurismo.ArchitectureDocumentation;

/// <summary>A representative table and its architecture classifications.</summary>
/// <param name="Schema">The PostgreSQL schema.</param>
/// <param name="Name">The PostgreSQL table name.</param>
/// <param name="Role">The table's persistence role.</param>
/// <param name="DataClasses">The data handling classes represented by the table.</param>
internal sealed record ArchitectureTable(string Schema, string Name, string Role, string[] DataClasses);
