namespace ViajantesTurismo.Admin.BehaviorTests.Context;

/// <summary>
/// Helper methods for setting up common test data in context objects.
/// Use these to reduce duplication while keeping contexts clean and explicit.
/// </summary>
public static class ContextHelpers
{
    /// <summary>
    /// Maps a scenario customer number to a stable, deterministic customer id.
    /// </summary>
    public static Guid CustomerGuid(int scenarioId) => new(scenarioId, 0, 0, new byte[8]);

    /// <summary>
    /// Sets up a valid tour with common test values.
    /// </summary>
    public static void SetupValidTour(TourContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Identifier = "TEST2024";
        context.Name = "Test Tour";
        context.StartDate = TimeProvider.System.GetUtcNow().UtcDateTime.AddMonths(1);
        context.EndDate = TimeProvider.System.GetUtcNow().UtcDateTime.AddMonths(1).AddDays(7);
        context.BasePrice = 2000.00m;
        context.SingleRoomSupplementPrice = 500.00m;
        context.RegularBikePrice = 100.00m;
        context.EBikePrice = 200.00m;
    }
}
