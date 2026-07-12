using Projects;
using SharedKernel.IntegrationTesting;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

internal sealed class ConcurrentAspireTestApplications : IAsyncDisposable
{
    private ConcurrentAspireTestApplications(AspireTestApplication first, AspireTestApplication second)
    {
        First = first;
        Second = second;
    }

    public AspireTestApplication First { get; }

    public AspireTestApplication Second { get; }

    public static async Task<ConcurrentAspireTestApplications> Start(CancellationToken ct)
    {
        var firstStart = AspireTestApplication.Start<ViajantesTurismo_AppHost>(
            [ResourceNames.Api],
            null,
            AppHostTestArguments.Create(),
            ct);
        var secondStart = AspireTestApplication.Start<ViajantesTurismo_AppHost>(
            [ResourceNames.Api],
            null,
            AppHostTestArguments.Create(),
            ct);

        try
        {
            var applications = await Task.WhenAll(firstStart, secondStart);
            return new ConcurrentAspireTestApplications(applications[0], applications[1]);
        }
        catch
        {
            if (firstStart.IsCompletedSuccessfully)
            {
                var first = await firstStart;
                await first.DisposeAsync();
            }

            if (secondStart.IsCompletedSuccessfully)
            {
                var second = await secondStart;
                await second.DisposeAsync();
            }

            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Second.DisposeAsync();
        await First.DisposeAsync();
    }
}
