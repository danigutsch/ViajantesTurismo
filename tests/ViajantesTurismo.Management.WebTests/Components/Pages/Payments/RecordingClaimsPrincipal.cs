using System.Security.Claims;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Payments;

/// <summary>Admin principal with a subject claim that counts how often a claim lookup reads it.</summary>
/// <remarks>Creates an authenticated administrator with the given subject id.</remarks>
internal sealed class RecordingClaimsPrincipal(string subject) : ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", subject), new Claim(ClaimTypes.Role, "Admin")],
            "test",
            ClaimTypes.Name,
            ClaimTypes.Role))
{

    /// <summary>Gets the number of single-claim lookups made against this principal.</summary>
    public int ClaimLookups
    {
        get; private set;
    }

    /// <inheritdoc />
    public override Claim? FindFirst(string type)
    {
        ClaimLookups++;
        return base.FindFirst(type);
    }
}
