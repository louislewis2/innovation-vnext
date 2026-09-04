namespace Innovation.ServiceBus.InProcess.vNext.Reactions
{
    using System.Threading;
    using System.Threading.Tasks;
    using System.Diagnostics.CodeAnalysis;

    using Api.vNext.Reactions;

    /// <summary>
    /// Fixed, library-owned logic for actually executing a <see cref="ReactorWorkItem"/>: creates a fresh
    /// scope, resolves the registered reactor(s) for the work item's contract type, and invokes them.
    /// This logic is shared by every <see cref="IReactorWorkQueue"/> implementation - including custom or
    /// durable ones - so reactor resolution/invocation semantics stay consistent no matter where the work
    /// item came from.
    /// </summary>
    public interface IReactorInvoker
    {
        ValueTask InvokeAsync([DisallowNull] ReactorWorkItem item, CancellationToken cancellationToken = default);
    }
}
