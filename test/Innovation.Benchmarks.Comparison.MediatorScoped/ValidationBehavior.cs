namespace Innovation.Benchmarks.Comparison.MediatorScoped
{
    using System.Threading;
    using System.Threading.Tasks;

    using global::Mediator;

    /// <summary>
    /// No-op validation pipeline behavior, kept IO-free - always passes and calls next(). Identical
    /// to MediatorLib.ValidationBehavior, duplicated here because this project cannot reference
    /// MediatorLib (see AssemblyInfo.cs - generator collision across differently-configured Mediator
    /// assemblies).
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
