namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Represents one classified field in a generated document draft.</summary>
public sealed record GetDocumentFieldDto
{
    /// <summary>Gets the stable template field identifier.</summary>
    public required string FieldId { get; init; }

    /// <summary>Gets the staff-facing field label.</summary>
    public required string Label { get; init; }

    /// <summary>Gets the value currently rendered into the document.</summary>
    public required string RenderedValue { get; init; }

    /// <summary>Gets whether authorized staff may override the field.</summary>
    public required bool IsEditable { get; init; }
}
