namespace Innovation.ServiceBus.InProcess.vNext.Reactions
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using System.Collections.Concurrent;
    using Microsoft.Extensions.DependencyInjection;

    using Api.vNext.Core;
    using Api.vNext.Reactions;

    /// <summary>
    /// Default <see cref="IReactorInvoker"/> implementation. Always resolves reactors from a fresh
    /// <see cref="IServiceScope"/> created just for this invocation - never the scope the original
    /// command was dispatched under, which may already be disposed (e.g. an ASP.NET Core request scope
    /// disposed at the end of the HTTP request) by the time this runs in the background.
    /// </summary>
    public sealed class ReactorInvoker : IReactorInvoker
    {
        #region Fields

        // Caches the reflected React MethodInfo per reactor contract type, so repeated invocations of the
        // same reactor contract don't repeatedly pay for a string-based GetMethod lookup. This still uses
        // MethodInfo.Invoke (not a compiled expression-tree delegate) since this only runs on the
        // background reactor drain path, never the synchronous dispatch hot path.
        private static readonly ConcurrentDictionary<Type, MethodInfo> reactMethodCache = new ConcurrentDictionary<Type, MethodInfo>();

        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<ReactorInvoker> logger;

        #endregion Fields

        #region Constructor

        public ReactorInvoker(IServiceScopeFactory serviceScopeFactory, ILogger<ReactorInvoker> logger)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
        }

        #endregion Constructor

        #region Methods

        public async ValueTask InvokeAsync(ReactorWorkItem item, CancellationToken cancellationToken = default)
        {
            using var scope = this.serviceScopeFactory.CreateScope();

            var reactors = scope.ServiceProvider.GetServices(serviceType: item.ReactorContractType).ToArray();

            if (reactors.Length == 0)
            {
                return;
            }

            var reactMethod = reactMethodCache.GetOrAdd(key: item.ReactorContractType, valueFactory: static contractType => contractType.GetMethod(name: "React"));
            var isResultReactor = item.CommandResult != null;
            var arguments = isResultReactor ? new object[] { item.CommandResult, item.Command } : new object[] { item.Command };

            foreach (var reactor in reactors)
            {
                if (reactor == null)
                {
                    continue;
                }

                try
                {
                    // Reactors run in the background, long after the dispatch that queued them returned, so
                    // they have no other route to the originating correlation id - the React contracts only
                    // receive the command (and result). Setting it here keeps correlation on the component
                    // rather than requiring it to be smuggled through the command itself.
                    if (reactor is ICorrelationAware correlationAwareReactor)
                    {
                        correlationAwareReactor.CorrelationId = item.CorrelationId;
                    }

                    if (reactMethod.Invoke(obj: reactor, parameters: arguments) is Task reactTask)
                    {
                        await reactTask;
                    }
                }
                catch (Exception ex)
                {
                    // React methods can throw synchronously (before returning their Task), in which case
                    // reflection wraps the real exception in a TargetInvocationException - unwrap it so
                    // logs show the actual cause rather than the reflection plumbing.
                    var actualException = ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;

                    this.logger.LogError(
                        exception: actualException,
                        message: "The Reactor: {ReactorType} Raised An Exception. CorrelationId: {CorrelationId}",
                        reactor.GetType(),
                        item.CorrelationId);
                }
            }
        }

        #endregion Methods
    }
}
