using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Supplies an authorized staff override for one editable document field.</summary>
public sealed record UpdateDocumentFieldDto
{
    /// <summary>Gets the replacement field value.</summary>
    [Required]
    [StringLength(ContractConstants.MaxDocumentFieldValueLength)]
    public required string Value { get; init; }
}
