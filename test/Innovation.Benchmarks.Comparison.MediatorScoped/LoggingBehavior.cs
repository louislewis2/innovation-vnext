namespace Innovation.Benchmarks.Comparison.MediatorScoped
{
    using System.Threading;
    using System.Threading.Tasks;

    using global::Mediator;

    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Logging pipeline behavior, identical to MediatorLib.LoggingBehavior - actually resolves and
    /// calls an <see cref="ILogger{TCategoryName}"/> (guarded by IsEnabled(Debug)) rather than being a
    /// no-op. Duplicated here rather than referenced because this project cannot reference MediatorLib
    /// (see AssemblyInfo.cs - generator collision across differently-configured Mediator assemblies).
    /// Registered Singleton here (see MediatorScopedLoadedProvider), same as MediatorLoadedProvider -
    /// the behavior's own DI lifetime is independent of the assembly-wide
    /// MediatorOptions.ServiceLifetime setting being measured, so it's kept identical to the Singleton
    /// comparison for a like-for-like pipeline behavior configuration.
    /// </summary>
    public class LoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
        where TMessage : IMessage
    {
        private readonly ILogger<LoggingBehavior<TMessage, TResponse>> logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
        {
            this.logger = logger;
        }

        public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
        {
            if (this.logger.IsEnabled(LogLevel.Debug))
            {
                this.logger.LogDebug("Handling {MessageType}", typeof(TMessage).Name);
            }

            var response = await next(message, cancellationToken);

            if (this.logger.IsEnabled(LogLevel.Debug))
            {
                this.logger.LogDebug("Handled {MessageType}", typeof(TMessage).Name);
            }

            return response;
        }
    }
}
