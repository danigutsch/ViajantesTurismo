using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SharedKernel.ServiceDefaults;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.ServiceDefaults;

/// <summary>Applies the application policy for planned HTTP maintenance.</summary>
public static class HttpMaintenanceExtensions
{
    private const string MaintenanceTitle = "Scheduled maintenance";
    private const string MaintenancePage = """
        <!doctype html>
        <html lang="en">
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>Scheduled maintenance</title>
        </head>
        <body>
            <main>
                <h1>Scheduled maintenance</h1>
                <p>Please try again later.</p>
            </main>
        </body>
        </html>
        """;

    /// <summary>Reads the startup switch and makes readiness unhealthy during planned maintenance.</summary>
    /// <param name="builder">The HTTP host builder.</param>
    /// <returns>The validated startup switch for the request pipeline.</returns>
    public static bool AddHttpMaintenanceReadiness(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var enabled = HttpMaintenanceConfiguration.IsEnabled(builder.Configuration[HttpMaintenanceConfiguration.EnabledKey]);
        if (enabled)
        {
            builder.Services.AddHealthChecks().AddCheck("planned-maintenance", static () => HealthCheckResult.Unhealthy());
        }

        return enabled;
    }

    /// <summary>Returns a maintenance response for application requests and preserves the configured health endpoints.</summary>
    /// <param name="app">The HTTP host.</param>
    /// <param name="enabled">The validated startup switch used to configure readiness.</param>
    /// <param name="browser">Whether the host serves a browser page.</param>
    public static void UseHttpMaintenance(this WebApplication app, bool enabled, bool browser)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (!enabled)
        {
            return;
        }

        var health = app.Services.GetRequiredService<ServiceDefaultsOptions>();
        var readiness = new PathString(health.HealthPath);
        var liveness = new PathString(health.AlivePath);
        app.UseWhen(context => context.Request.Path != readiness && context.Request.Path != liveness,
            branch => branch.Run(browser ? WritePage : WriteProblem));
    }

    private static Task WriteProblem(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: MaintenanceTitle).ExecuteAsync(context);
    }

    private static Task WritePage(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.CacheControl = "no-store";
        context.Response.ContentType = "text/html; charset=utf-8";
        return context.Response.WriteAsync(MaintenancePage, context.RequestAborted);
    }
}
