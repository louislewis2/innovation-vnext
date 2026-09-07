namespace Innovation.Benchmarks.Comparison.MediatRLib
{
    using System.Threading;
    using System.Threading.Tasks;

    using MediatR;

    /// <summary>
    /// No-op validation pipeline behavior, kept IO-free - always passes and calls next(). Mirrors
    /// Innovation's LoadedCommandValidator/IValidator&lt;T&gt;, registered as an open generic exactly as
    /// MediatR's own documentation recommends for cross-cutting concerns.
    /// </summary>
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            return next(cancellationToken);
        }
    }
}
