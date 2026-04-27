namespace SharedKernel.Mediator;

/// <summary>
/// Provides the runtime mediator shell that generated DI and dispatch code compose around.
/// </summary>
public sealed partial class AppMediator : IMediator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppMediator"/> class.
    /// </summary>
    /// <param name="services">The scoped service provider used by generated dispatch code.</param>
    public AppMediator(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        Services = services;
    }

    /// <summary>
    /// Gets the scoped service provider used by generated dispatch and publication code.
    /// </summary>
    internal IServiceProvider Services { get; }

    /// <inheritdoc />
    public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        throw new NotSupportedException("Generated request dispatch is not available yet.");
    }

    /// <inheritdoc />
    public ValueTask Publish<TNotification>(TNotification notification, CancellationToken ct)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        throw new NotSupportedException("Generated notification dispatch is not available yet.");
    }
}
