using JetBrains.Annotations;
using ViajantesTurismo.Common;

namespace ViajantesTurismo.Admin.Domain.Customers;

/// <summary>
/// Represents identification information of a customer.
/// </summary>
public sealed class IdentificationInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdentificationInfo"/> class.
    /// </summary>
    /// <param name="nationalId">The national ID.</param>
    /// <param name="idNationality">The nationality that issued the ID.</param>
    public IdentificationInfo(string nationalId, string idNationality)
    {
        NationalId = StringSanitizer.Sanitize(nationalId);
        IdNationality = StringSanitizer.Sanitize(idNationality);
    }

    /// <summary>National ID.</summary>
    public string NationalId { get; private set; }

    /// <summary>The nationality that issued the ID.</summary>
    public string IdNationality { get; private set; }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
#pragma warning disable CS8618
    [UsedImplicitly]
    private IdentificationInfo()
    {
    }
}
