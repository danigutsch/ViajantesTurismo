namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Branding;

/// <summary>Reads only the durable delivery facts used to verify a reproduction attempt.</summary>
/// <param name="Processed">Whether the worker acknowledged the message.</param>
/// <param name="FailedAttempts">The number of failed delivery attempts.</param>
/// <param name="RetryAt">The next eligible retry time after a failure.</param>
/// <param name="ErrorType">The bounded last failure type.</param>
/// <param name="Poisoned">Whether ordinary delivery exhausted the attempt limit.</param>
/// <param name="Claimed">Whether the transport row has a live claim marker.</param>
internal readonly record struct BrandingWorkerDelivery(
    bool Processed, int FailedAttempts, DateTimeOffset? RetryAt, string? ErrorType, bool Poisoned, bool Claimed);
