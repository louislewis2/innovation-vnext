namespace Innovation.Api.vNext.Reactions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Optional capability for an <see cref="IReactorWorkQueue"/> that holds work in memory: gives a
    /// caller (e.g. a hosted service on application shutdown) a chance to let already-queued work finish
    /// before the process exits. Deliberately not part of <see cref="IReactorWorkQueue"/> itself, because
    /// a durable implementation (that persists work immediately on enqueue) has nothing in memory to
    /// drain in the first place.
    /// </summary>
    public interface IDrainableReactorWorkQueue
    {
        /// <summary>
        /// Stops accepting new work and waits for already-queued/in-flight work to finish, up to
        /// <paramref name="timeout"/>. Any work still queued once the timeout elapses is discarded.
        /// </summary>
        ValueTask DrainAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
    }
}
