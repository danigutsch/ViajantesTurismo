using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PandaBear.Testing.Snapshots;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Testing.Reproduction;

namespace ViajantesTurismo.Admin.ApiServiceTests.Tours;

/// <summary>Recreates a synthetic tour and runs retirement through the real Admin API boundary.</summary>
internal static class TourRetirementReproductionScenario
{
    /// <summary>The approved result for a pending Catalog withdrawal.</summary>
    internal const string Accepted = "accepted-pending";

    /// <summary>The approved result for remaining active bookings.</summary>
    internal const string Conflict = "active-bookings-conflict";

    /// <summary>The approved result for an authenticated Reader.</summary>
    internal const string Denied = "forbidden";

    /// <summary>Gets the clock used by the application for each independent reset.</summary>
    internal static DateTimeOffset FixtureUtc { get; } = new(2030, 1, 2, 10, 0, 0, TimeSpan.Zero);

    /// <summary>Registers the fixture and its three reviewed outcomes for the manifest codec.</summary>
    internal static ReproductionSpecification Specification
    {
        get;
    } = new(
        "admin.tour-retirement", ReproductionFixtureEnvironment.GetSourceRevision(typeof(TourRetirementReproductionScenario).Assembly),
        [Outcome(Accepted), Outcome(Conflict), Outcome(Denied)]);

    /// <summary>Maps a verified scenario outcome to the fixed resource-state vocabulary.</summary>
    internal static ReproductionOutcome Outcome(string code) =>
        new("reproduced", code, new Dictionary<string, string>(StringComparer.Ordinal) { ["admin-api"] = "ready" });

    /// <summary>Runs one attempt using a fresh host, store, clock, and test identity.</summary>
    internal static async Task<TourRetirementReproductionRun> Run(string role, bool activeBooking, CancellationToken ct)
    {
        var startedUtc = TimeProvider.System.GetUtcNow();
        var started = Stopwatch.GetTimestamp();
        var attemptId = Guid.NewGuid();
        using var activity = new Activity("admin.tour-retirement-reproduction");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.SetParentId(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
        activity.Start();
        var trace = new TourRetirementTraceObservation(activity.TraceId);

        var tour = EntityBuilders.BuildTour(new TourOptions(Identifier: "REPRO-RETIRE", Schedule: new TourScheduleOptions(
            StartDate: FixtureUtc.UtcDateTime.AddMonths(2), EndDate: FixtureUtc.UtcDateTime.AddMonths(2).AddDays(7))));
        Booking? booking = activeBooking
            ? BookingTestHelpers.AddSingleCustomerBooking(tour, new SingleBookingOptions(CustomerId: Guid.NewGuid())).Value
            : null;
        var tours = new FakeTourStore();
        tours.Add(tour);
        var unitOfWork = new FakeUnitOfWork();
        HttpStatusCode retirementStatus;
        HttpStatusCode queryStatus;
        bool queryNoStore;
        GetTourRetirementDto? retirement;

        await using (var factory = AdminApiTestHost.Create(services =>
        {
            services.AddSingleton<IStartupFilter>(trace);
            services.Replace(ServiceDescriptor.Singleton<TimeProvider>(new FakeTimeProvider(FixtureUtc)));
            services.Replace(ServiceDescriptor.Singleton<ITourCommandStore>(tours));
            services.Replace(ServiceDescriptor.Singleton<IUnitOfWork>(unitOfWork));
            services.Replace(ServiceDescriptor.Singleton<IQueryService>(new TourRetirementQueries(tour, completedAt: null)));
        }))
        {
            using var client = factory.CreateClient();
            AdminApiTestHost.ConfigureAuthenticatedClient(client, role);
            if (!client.DefaultRequestHeaders.TryAddWithoutValidation("traceparent", activity.Id))
            {
                throw new InvalidOperationException("The test client could not carry the attempt trace context.");
            }

            using var deletion = await client.DeleteAsync(new Uri($"/api/v1/tours/{tour.Id}", UriKind.Relative), ct);
            using var status = await client.GetAsync(new Uri($"/api/v1/tours/{tour.Id}/retirement", UriKind.Relative), ct);
            retirementStatus = deletion.StatusCode;
            queryStatus = status.StatusCode;
            queryNoStore = status.Headers.CacheControl?.NoStore == true;
            retirement = queryStatus == HttpStatusCode.OK
                ? await status.Content.ReadFromJsonAsync<GetTourRetirementDto>(ct)
                : null;
        }

        return new TourRetirementReproductionRun(
            tour.Id, retirementStatus, queryStatus, retirement, queryNoStore, tour.IsRetired, unitOfWork.SaveEntitiesCallCount,
            booking?.Status, attemptId, trace.ObservedTrace, startedUtc,
            checked((int)Stopwatch.GetElapsedTime(started).TotalMilliseconds));
    }
}
