using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>
/// Requests a disposition hold on a document lineage. Every text field is an opaque code: letters, digits and
/// <c>. _ : -</c> only, never names, e-mail addresses or free text. The placing actor comes from the authenticated
/// principal and is not part of the request.
/// </summary>
public sealed record CreateDocumentHoldDto
{
    /// <summary>Gets why the hold exists: <c>LegalClaim</c>, <c>LegalObligation</c>, <c>DataSubjectRestriction</c>, <c>RegulatoryInquiry</c> or <c>Investigation</c>.</summary>
    [Required]
    [AllowedValues(
        ContractConstants.DocumentHoldBasisLegalClaim,
        ContractConstants.DocumentHoldBasisLegalObligation,
        ContractConstants.DocumentHoldBasisDataSubjectRestriction,
        ContractConstants.DocumentHoldBasisRegulatoryInquiry,
        ContractConstants.DocumentHoldBasisInvestigation)]
    public required string Basis
    {
        get; init;
    }

    /// <summary>Gets the opaque purpose code the hold serves.</summary>
    [Required]
    [StringLength(ContractConstants.MaxDocumentHoldCodeLength)]
    [RegularExpression(ContractConstants.DocumentHoldCodePattern)]
    public required string PurposeCode
    {
        get; init;
    }

    /// <summary>Gets the opaque reference to the restricted record that authorizes the hold.</summary>
    [Required]
    [StringLength(ContractConstants.MaxDocumentHoldCodeLength)]
    [RegularExpression(ContractConstants.DocumentHoldCodePattern)]
    public required string AuthorityReference
    {
        get; init;
    }

    /// <summary>Gets when the hold is next due for review; it must be in the future.</summary>
    public required DateTimeOffset ReviewDueAt
    {
        get; init;
    }

    /// <summary>Gets when the hold stops restraining disposition by itself, or <see langword="null" /> when it needs an explicit release.</summary>
    public DateTimeOffset? ExpiresAt
    {
        get; init;
    }
}
