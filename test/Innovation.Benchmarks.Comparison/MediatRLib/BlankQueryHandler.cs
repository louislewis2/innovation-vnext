namespace Innovation.Benchmarks.Comparison.MediatRLib
{
    using System.Threading;
    using System.Threading.Tasks;

    using MediatR;

    /// <summary>
    /// Handles BlankQuery with no work, kept IO-free so the benchmark measures only dispatch overhead.
    /// </summary>
    public class BlankQueryHandler : IRequestHandler<BlankQuery, BlankQueryResult>
    {
        private static readonly BlankQueryResult queryResult = new BlankQueryResult();

        public Task<BlankQueryResult> Handle(BlankQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(queryResult);
        }
    }
}
