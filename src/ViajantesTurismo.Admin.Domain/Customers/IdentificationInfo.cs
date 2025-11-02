using JetBrains.Annotations;
using ViajantesTurismo.AdminApi.Contracts;
using ViajantesTurismo.Common.Results;
using ViajantesTurismo.Common.Sanitizers;
using static ViajantesTurismo.Admin.Domain.Customers.CustomerErrors;

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
    private IdentificationInfo(string nationalId, string idNationality)
    {
        NationalId = nationalId;
        IdNationality = idNationality;
    }

    /// <summary>National ID.</summary>
    public string NationalId { get; private set; }

    /// <summary>The nationality that issued the ID.</summary>
    public string IdNationality { get; private set; }

    /// <summary>
    /// Creates a new instance of <see cref="IdentificationInfo"/> with validation.
    /// </summary>
    /// <param name="nationalId">The national ID.</param>
    /// <param name="idNationality">The nationality that issued the ID.</param>
    /// <returns>A <see cref="Result{IdentificationInfo}"/> containing the identification info or validation errors.</returns>
    public static Result<IdentificationInfo> Create(string? nationalId, string? idNationality)
    {
        var sanitizedNationalId = StringSanitizer.Sanitize(nationalId);
        var sanitizedIdNationality = StringSanitizer.Sanitize(idNationality);

        var errors = new ValidationErrors();

        if (string.IsNullOrWhiteSpace(sanitizedNationalId))
        {
            errors.Add(EmptyNationalId());
        }
        else if (sanitizedNationalId.Length > ContractConstants.MaxDefaultLength)
        {
            errors.Add(NationalIdTooLong());
        }

        if (string.IsNullOrWhiteSpace(sanitizedIdNationality))
        {
            errors.Add(EmptyIdNationality());
        }
        else if (sanitizedIdNationality.Length > ContractConstants.MaxDefaultLength)
        {
            errors.Add(IdNationalityTooLong());
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<IdentificationInfo>();
        }

        return new IdentificationInfo(sanitizedNationalId!, sanitizedIdNationality!);
    }

    /// <summary>
    /// DO NOT USE. This constructor is required by Entity Framework Core for materialization.
    /// </summary>
#pragma warning disable CS8618
    [UsedImplicitly]
    private IdentificationInfo()
    {
    }
}