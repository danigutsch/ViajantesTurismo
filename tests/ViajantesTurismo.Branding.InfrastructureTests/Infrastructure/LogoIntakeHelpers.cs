using Microsoft.Extensions.Logging.Abstractions;
using PandaBear.Results;
using ViajantesTurismo.Branding.Infrastructure;

namespace ViajantesTurismo.Branding.InfrastructureTests.Infrastructure;

/// <summary>Creates the logo intake under test and reads its refusals.</summary>
internal static class LogoIntakeHelpers
{
    /// <summary>Creates the intake with the given scanner and a no-op logger.</summary>
    public static BrandingLogoIntake Create(RecordingMalwareScanner scanner) => new(scanner, NullLogger<BrandingLogoIntake>.Instance);

    /// <summary>Returns the single message the refusal reports for the <c>Content</c> field.</summary>
    public static string ContentError(Result<BrandingLogo> result)
    {
        return result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull()["Content"].ShouldHaveSingleItem();
    }
}
