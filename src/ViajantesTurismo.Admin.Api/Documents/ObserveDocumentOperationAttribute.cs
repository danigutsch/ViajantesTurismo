using ViajantesTurismo.Admin.Infrastructure.Documents;

namespace ViajantesTurismo.Admin.Api.Documents;

/// <summary>Identifies the bounded operation that the route group records for a handler.</summary>
/// <param name="operation">The operation recorded in the document signals.</param>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class ObserveDocumentOperationAttribute(DocumentOperation operation) : Attribute
{
    /// <summary>Gets the operation recorded in the document signals.</summary>
    public DocumentOperation Operation { get; } = operation;
}
