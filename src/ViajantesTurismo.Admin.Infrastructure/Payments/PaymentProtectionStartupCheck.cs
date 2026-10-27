using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Admin.Application.Payments;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

/// <summary>Hosted service that forces payment protection and calendar services to resolve at startup.</summary>
/// <param name="protection">The payment protection service.</param>
/// <param name="calendar">The payment business calendar.</param>
internal sealed class PaymentProtectionStartupCheck(PaymentProtection protection, PaymentBusinessCalendar calendar) : IHostedService
{
    /// <summary>Touches the injected services so misconfiguration fails at startup.</summary>
    public Task StartAsync(CancellationToken ct)
    {
        _ = protection.Enabled;
        _ = calendar;
        return Task.CompletedTask;
    }
    /// <summary>Does nothing on shutdown.</summary>
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
