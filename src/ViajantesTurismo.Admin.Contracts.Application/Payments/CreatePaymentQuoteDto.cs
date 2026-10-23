using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Records a manually agreed BRL-per-EUR quote without approving it.</summary>
public sealed record CreatePaymentQuoteDto
{
    /// <summary>Gets ExpectedVersion.</summary>
    [Range(0, long.MaxValue)]
    public required long ExpectedVersion
    {
        get; init;
    }
    /// <summary>Gets BrlPerEuro.</summary>
    public required decimal BrlPerEuro
    {
        get; init;
    }
    /// <summary>Gets Source.</summary>
    [Required]
    [StringLength(128)]
    public required string Source
    {
        get; init;
    }
    /// <summary>Gets Reference.</summary>
    [Required]
    [StringLength(128)]
    public required string Reference
    {
        get; init;
    }
    /// <summary>Gets AgreedAt.</summary>
    public required DateTimeOffset AgreedAt
    {
        get; init;
    }
    /// <summary>Gets ExpiresAt.</summary>
    public required DateTimeOffset ExpiresAt
    {
        get; init;
    }
    /// <summary>Gets ForRefund.</summary>
    public required bool ForRefund
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "CreatePaymentQuoteDto [redacted]";
}
