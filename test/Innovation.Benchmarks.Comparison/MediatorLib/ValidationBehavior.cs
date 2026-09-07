namespace Innovation.Benchmarks.Comparison.MediatorLib
{
    using System.Threading;
    using System.Threading.Tasks;

    using global::Mediator;

    /// <summary>
    /// No-op validation pipeline behavior, kept IO-free - always passes and calls next(). Mirrors
    /// Innovation's LoadedCommandValidator/IValidator&lt;T&gt;, registered as an open generic exactly as
    /// documented in Mediator's README (section 3.3.1. Message validation example).
    /// </summary>
    public class ValidationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
        where TMessage : IMessage
    {
        public ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
        {
            return next(message, cancellationToken);
        }
    }
}
