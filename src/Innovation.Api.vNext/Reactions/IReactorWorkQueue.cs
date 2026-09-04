namespace Innovation.Api.vNext.Reactions
{
    using System.Threading;
    using System.Threading.Tasks;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// The extensibility seam for scheduling reactor work (both <see cref="ICommandReactor{TCommand}"/> and
    /// <see cref="ICommandResultReactor{TCommand}"/>). The default implementation shipped with the service
    /// bus library is a safe, in-memory, best-effort queue - work still queued or in-flight when the
    /// process terminates without a graceful drain is lost.
    /// </summary>
    /// <remarks>
    /// Implement this interface yourself, and register it via
    /// <c>services.AddInnovationvNext().WithReactorWorkQueue&lt;TYourQueue&gt;()</c>, if you need
    /// at-least-once/durable delivery guarantees across process restarts - for example by persisting the
    /// <see cref="ReactorWorkItem"/> to a database outbox table or a durable queue (SQS, Kafka, etc.)
    /// before <see cref="EnqueueAsync"/> returns. This library intentionally does not attempt to provide
    /// that guarantee itself, since it is really an Outbox-style concern best solved alongside your own
    /// persistence layer.
    /// </remarks>
    public interface IReactorWorkQueue
    {
        ValueTask EnqueueAsync([DisallowNull] ReactorWorkItem item, CancellationToken cancellationToken = default);
    }
}
