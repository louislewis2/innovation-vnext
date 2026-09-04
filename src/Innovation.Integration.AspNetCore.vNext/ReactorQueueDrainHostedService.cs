namespace Innovation.Integration.AspNetCore.vNext
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    using Innovation.Api.vNext.Reactions;
    using Innovation.ServiceBus.InProcess.vNext.Settings;

    /// <summary>
    /// On graceful ASP.NET Core shutdown, gives the registered <see cref="IReactorWorkQueue"/> a chance to
    /// finish already-queued/in-flight reactor work before the process exits, if (and only if) it opts
    /// into <see cref="IDrainableReactorWorkQueue"/>. A custom, durable IReactorWorkQueue that persists
    /// work immediately on enqueue has nothing to drain and simply won't implement that interface - this
    /// hosted service is then a no-op for it. Registered via TryAddEnumerable so this cannot be
    /// accidentally registered more than once.
    /// </summary>
    /// <remarks>
    /// This type lives here, not in the host-agnostic Innovation.ServiceBus.InProcess.vNext library,
    /// because IHostedService/the Generic Host is only guaranteed to be present when running on
    /// ASP.NET Core (or another Generic-Host-based host) - the service bus library itself must also work
    /// unmodified in a plain console application with no host at all.
    /// </remarks>
    internal sealed class ReactorQueueDrainHostedService : IHostedService
    {
        #region Fields

        private readonly IReactorWorkQueue reactorWorkQueue;
        private readonly InnovationOptions innovationOptions;
        private readonly ILogger<ReactorQueueDrainHostedService> logger;

        #endregion Fields

        #region Constructor

        public ReactorQueueDrainHostedService(IReactorWorkQueue reactorWorkQueue, IOptions<InnovationOptions> innovationOptions, ILogger<ReactorQueueDrainHostedService> logger)
        {
            this.reactorWorkQueue = reactorWorkQueue;
            this.innovationOptions = innovationOptions.Value;
            this.logger = logger;
        }

        #endregion Constructor

        #region Methods

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (this.reactorWorkQueue is not IDrainableReactorWorkQueue drainableReactorWorkQueue)
            {
                return;
            }

            this.logger.LogInformation(message: "Draining Reactor Work Queue Before Shutdown.");

            await drainableReactorWorkQueue.DrainAsync(timeout: this.innovationOptions.ReactorQueueShutdownDrainTimeout, cancellationToken: cancellationToken);
        }

        #endregion Methods
    }
}
