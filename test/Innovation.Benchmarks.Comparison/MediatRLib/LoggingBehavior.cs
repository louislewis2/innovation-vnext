namespace Innovation.Benchmarks.Comparison.MediatRLib
{
    using System.Threading;
    using System.Threading.Tasks;

    using MediatR;

    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Logging pipeline behavior, registered as an open generic exactly as MediatR's own documentation
    /// recommends for cross-cutting concerns (see 3.3.2. Error logging example in Mediator's README,
    /// same shape applies to MediatR). Actually resolves and calls an <see cref="ILogger{TCategoryName}"/>
    /// (guarded by <see cref="ILogger.IsEnabled(LogLevel)"/> before each entry/exit log, exactly like
    /// Innovation's dispatcher does internally) rather than being a pure no-op, so the "loaded" comparison
    /// measures an equivalent cost for a logging cross-cutting concern rather than an empty delegate hop.
    /// The behavior itself is registered Singleton (see MediatRLoadedProvider), so the ILogger is resolved
    /// once, not per-dispatch - only the guarded log calls happen per-dispatch.
    /// </summary>
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            this.logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (this.logger.IsEnabled(LogLevel.Debug))
            {
                this.logger.LogDebug("Handling {RequestType}", typeof(TRequest).Name);
            }

            var response = await next(cancellationToken);

            if (this.logger.IsEnabled(LogLevel.Debug))
            {
                this.logger.LogDebug("Handled {RequestType}", typeof(TRequest).Name);
            }

            return response;
        }
    }
}
