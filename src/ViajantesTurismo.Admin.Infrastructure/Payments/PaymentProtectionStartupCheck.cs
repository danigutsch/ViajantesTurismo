using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Admin.Application.Payments;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

internal sealed class PaymentProtectionStartupCheck(PaymentProtection protection, PaymentBusinessCalendar calendar) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = protection.Enabled;
        _ = calendar;
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
