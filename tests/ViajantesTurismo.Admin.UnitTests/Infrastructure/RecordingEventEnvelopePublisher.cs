using SharedKernel.Messaging;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal sealed class RecordingEventEnvelopePublisher : IEventEnvelopePublisher
{
    public List<EventEnvelope> Published { get; } = [];

    public Exception? Failure { get; set; }

    public ValueTask Publish(EventEnvelope envelope, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (Failure is not null)
        {
            throw Failure;
        }

        Published.Add(envelope);

        return ValueTask.CompletedTask;
    }
}
