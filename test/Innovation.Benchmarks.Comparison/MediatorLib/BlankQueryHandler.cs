namespace Innovation.Benchmarks.Comparison.MediatorLib
{
    using System.Threading;
    using System.Threading.Tasks;

    using global::Mediator;

    /// <summary>
    /// Handles BlankQuery with no work, kept IO-free so the benchmark measures only dispatch overhead.
    /// </summary>
    public class BlankQueryHandler : IQueryHandler<BlankQuery, BlankQueryResult>
    {
        private static readonly BlankQueryResult queryResult = new BlankQueryResult();

        public ValueTask<BlankQueryResult> Handle(BlankQuery query, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(queryResult);
        }
    }
}
