namespace Innovation.Benchmarks.Comparison.MediatorLib
{
    using System.Threading;
    using System.Threading.Tasks;

    using global::Mediator;

    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Logging pipeline behavior, registered as an open generic exactly as documented in Mediator's
    /// README (section 3.3.2. Error logging example). Actually resolves and calls an
    /// <see cref="ILogger{TCategoryName}"/> (guarded by <see cref="ILogger.IsEnabled(LogLevel)"/> before
    /// each entry/exit log, exactly like Innovation's dispatcher does internally) rather than being a
    /// pure no-op, so the "loaded" comparison measures an equivalent cost for a logging cross-cutting
    /// concern rather than an empty delegate hop. The behavior itself is registered Singleton (see
    /// MediatorLoadedProvider), so the ILogger is resolved once, not per-dispatch - only the guarded log
    /// calls happen per-dispatch.
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
