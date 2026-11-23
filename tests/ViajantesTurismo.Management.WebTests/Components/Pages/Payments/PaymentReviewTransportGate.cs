namespace ViajantesTurismo.Management.WebTests.Components.Pages.Payments;

/// <summary>Holds one instruction review request open until its client cancels it.</summary>
internal sealed class PaymentReviewTransportGate(HttpMessageHandler inner) : DelegatingHandler(inner)
{
    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _canceled = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets or sets whether the history page request should remain in flight.</summary>
    internal bool HoldReviewRequest
    {
        get; set;
    }

    /// <summary>Gets a signal that the history page request is in flight.</summary>
    internal Task Started => _started.Task;

    /// <summary>Gets a signal that the history page request was canceled.</summary>
    internal Task Canceled => _canceled.Task;

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (HoldReviewRequest && request.RequestUri?.AbsolutePath.EndsWith("/payment-instructions/reviews", StringComparison.Ordinal) == true)
        {
            _started.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            }
            finally
            {
                _canceled.TrySetResult();
            }
        }

        return await base.SendAsync(request, ct);
    }
}
