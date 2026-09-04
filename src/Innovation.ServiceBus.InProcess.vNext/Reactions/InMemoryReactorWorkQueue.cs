namespace Innovation.ServiceBus.InProcess.vNext.Reactions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Channels;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    using Settings;
    using Api.vNext.Reactions;

    /// <summary>
    /// The default, best-effort <see cref="IReactorWorkQueue"/>. Work is held in an in-process bounded
    /// channel and processed by a single dedicated background loop that starts as soon as this instance
    /// is constructed - deliberately not dependent on IHostedService/Generic Host, since the service bus
    /// library must also work in plain console apps with no host at all. Work still queued (or in flight)
    /// when the process terminates without a call to <see cref="DrainAsync"/> is lost - this is a
    /// best-effort queue, not a durable one. Register a custom <see cref="IReactorWorkQueue"/> via
    /// <c>.WithReactorWorkQueue&lt;T&gt;()</c> if you need durability guarantees.
    /// </summary>
    public sealed class InMemoryReactorWorkQueue : IReactorWorkQueue, IDrainableReactorWorkQueue, IDisposable
    {
        #region Fields

        private readonly Channel<ReactorWorkItem> channel;
        private readonly IReactorInvoker reactorInvoker;
        private readonly ILogger<InMemoryReactorWorkQueue> logger;
        private readonly Task processingTask;

        #endregion Fields

        #region Constructor

        public InMemoryReactorWorkQueue(
            IOptions<InnovationOptions> options,
            IReactorInvoker reactorInvoker,
            ILogger<InMemoryReactorWorkQueue> logger)
        {
            this.reactorInvoker = reactorInvoker;
            this.logger = logger;

            var innovationOptions = options?.Value ?? new InnovationOptions();

            this.channel = Channel.CreateBounded<ReactorWorkItem>(new BoundedChannelOptions(capacity: innovationOptions.ReactorQueueCapacity)
            {
                FullMode = innovationOptions.ReactorQueueFullMode,
                SingleReader = true,
                SingleWriter = false,
            });

            // TaskCreationOptions.LongRunning requests a dedicated thread rather than a threadpool thread,
            // since this loop runs for the lifetime of the process/queue rather than completing quickly.
            this.processingTask = Task.Factory.StartNew(
                function: this.ProcessQueueAsync,
                cancellationToken: CancellationToken.None,
                creationOptions: TaskCreationOptions.LongRunning,
                scheduler: TaskScheduler.Default).Unwrap();
        }

        #endregion Constructor

        #region Methods

        public ValueTask EnqueueAsync(ReactorWorkItem item, CancellationToken cancellationToken = default)
        {
            return this.channel.Writer.WriteAsync(item: item, cancellationToken: cancellationToken);
        }

        public async ValueTask DrainAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            this.channel.Writer.TryComplete();

            using var timeoutCts = new CancellationTokenSource(delay: timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                await this.processingTask.WaitAsync(cancellationToken: linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                this.logger.LogWarning(message: "Reactor Work Queue Drain Timed Out After {Timeout}. Remaining Queued Work Was Discarded.", timeout);
            }
        }

        public void Dispose()
        {
            this.channel.Writer.TryComplete();
        }

        #endregion Methods

        #region Private Methods

        private async Task ProcessQueueAsync()
        {
            await foreach (var item in this.channel.Reader.ReadAllAsync())
            {
                try
                {
                    await this.reactorInvoker.InvokeAsync(item: item);
                }
                catch (Exception ex)
                {
                    // IReactorInvoker already catches/logs individual reactor failures - this is a
                    // last-resort guard so a bug in the invoker itself can never kill the processing loop.
                    this.logger.LogError(exception: ex, message: "Unexpected Error Processing Reactor Work Item. CorrelationId: {CorrelationId}", item.CorrelationId);
                }
            }
        }

        #endregion Private Methods
    }
}
