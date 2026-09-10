namespace Innovation.Api.vNext.Dispatching
{
    using System.Threading;
    using System.Threading.Tasks;
    using System.Diagnostics.CodeAnalysis;

    using Querying;
    using Messaging;
    using Commanding;

    /// <summary>
    /// This is the interface that a dispatcher must implement.
    /// It is the type that user code will request when they want to dispatch an event.
    /// </summary>
    public interface IDispatcher
    {
        void SetCorrelationId([DisallowNull] string correlationId);
        void SetContext([DisallowNull] IDispatcherContext dispatcherContext);
        ValueTask<ICommandResult> Command<TCommand>([DisallowNull] TCommand command, CancellationToken cancellationToken, bool suppressExceptions = true) where TCommand : ICommand;
        ValueTask Message<TMessage>([DisallowNull] TMessage message, CancellationToken cancellationToken) where TMessage : IMessage;
        ValueTask MessageFor<TMessage>([DisallowNull] TMessage message, CancellationToken cancellationToken, params string[] addresses) where TMessage : IMessage;
        ValueTask<TQueryResult> Query<TQuery, TQueryResult>([DisallowNull] TQuery query, CancellationToken cancellationToken)
            where TQueryResult : IQueryResult
            where TQuery : IQuery;
        ValueTask<TQueryResult> QueryFor<TQuery, TQueryResult>([DisallowNull] TQuery query, CancellationToken cancellationToken, params string[] addresses)
            where TQueryResult : IQueryResult
            where TQuery : IQuery;
    }
}