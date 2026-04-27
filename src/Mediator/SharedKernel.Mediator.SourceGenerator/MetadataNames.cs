namespace SharedKernel.Mediator.SourceGenerator;

/// <summary>
/// Provides metadata names for the abstractions used during discovery.
/// </summary>
internal static class MetadataNames
{
    public const string Request = "SharedKernel.Mediator.IRequest`1";
    public const string Command = "SharedKernel.Mediator.ICommand";
    public const string CommandOfResponse = "SharedKernel.Mediator.ICommand`1";
    public const string Query = "SharedKernel.Mediator.IQuery`1";
    public const string RequestHandler = "SharedKernel.Mediator.IRequestHandler`2";
    public const string CommandHandler = "SharedKernel.Mediator.ICommandHandler`1";
    public const string CommandHandlerOfResponse = "SharedKernel.Mediator.ICommandHandler`2";
    public const string QueryHandler = "SharedKernel.Mediator.IQueryHandler`2";
    public const string PipelineBehavior = "SharedKernel.Mediator.IPipelineBehavior`2";
    public const string PipelineOrderAttribute = "SharedKernel.Mediator.PipelineOrderAttribute";
    public const string Notification = "SharedKernel.Mediator.INotification";
    public const string NotificationHandler = "SharedKernel.Mediator.INotificationHandler`1";
    public const string StreamRequest = "SharedKernel.Mediator.IStreamRequest`1";
    public const string StreamRequestHandler = "SharedKernel.Mediator.IStreamRequestHandler`2";
    public const string MediatorModuleAttribute = "SharedKernel.Mediator.MediatorModuleAttribute";
}
