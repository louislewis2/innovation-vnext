namespace Innovation.Api.vNext.Querying
{
    using System.Threading;
    using System.Threading.Tasks;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// This interface is for query handlers.
    /// All query handlers need to implement this interface.
    /// </summary>
    /// <typeparam name="TQuery"></typeparam>
    /// <typeparam name="TQueryResult"></typeparam>
    public interface IQueryHandler<in TQuery, TQueryResult>
        where TQuery : IQuery
        where TQueryResult : IQueryResult
    {
        ValueTask<TQueryResult> Handle([DisallowNull] TQuery query, CancellationToken cancellationToken);
    }
}
