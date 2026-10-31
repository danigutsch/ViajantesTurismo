using System.Diagnostics;

namespace ViajantesTurismo.Admin.UnitTests.Application.Pipeline;

/// <summary>Helpers for <see cref="ResultTagBehaviorTests"/>.</summary>
internal static class ResultTagBehaviorTestsHelpers
{
    /// <summary>Creates a listener that samples every activity of <paramref name="expectedSource"/>.</summary>
    /// <param name="expectedSource">The only source to listen to.</param>
    /// <returns>The registered listener.</returns>
    public static ActivityListener CreateListener(ActivitySource expectedSource)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => ReferenceEquals(source, expectedSource),
            Sample = (ref _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
