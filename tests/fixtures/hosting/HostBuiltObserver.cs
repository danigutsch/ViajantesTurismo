using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace ViajantesTurismo.Testing.Hosting;

/// <summary>Subscribes to the hosting diagnostic listener and reports each host it announces as built.</summary>
/// <param name="onHostBuilt">Called with every built host; it may throw to stop the entry point.</param>
internal sealed class HostBuiltObserver(Action<IHost> onHostBuilt)
    : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
{
    private const string HostingListenerName = "Microsoft.Extensions.Hosting";
    private const string HostBuiltEventName = "HostBuilt";

    private readonly List<IDisposable> listenerSubscriptions = [];
    private readonly Lock gate = new();

    /// <inheritdoc />
    public void OnNext(DiagnosticListener value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Name != HostingListenerName)
        {
            return;
        }

        var subscription = value.Subscribe(this);
        lock (gate)
        {
            listenerSubscriptions.Add(subscription);
        }
    }

    /// <inheritdoc />
    public void OnNext(KeyValuePair<string, object?> value)
    {
        if (value.Key == HostBuiltEventName && value.Value is IHost host)
        {
            onHostBuilt(host);
        }
    }

    /// <inheritdoc />
    public void OnCompleted()
    {
    }

    /// <inheritdoc />
    public void OnError(Exception error)
    {
    }

    /// <summary>Removes the subscriptions to the hosting listeners.</summary>
    public void Dispose()
    {
        lock (gate)
        {
            foreach (var subscription in listenerSubscriptions)
            {
                subscription.Dispose();
            }

            listenerSubscriptions.Clear();
        }
    }
}
